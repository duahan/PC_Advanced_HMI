Imports MfgControl.AdvancedHMI.Drivers.Common
Imports MfgControl.AdvancedHMI.Drivers.Modbus

'******************************************************************************
'* Modbus TCP Protocol Implementation
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* support@advancedhmi.com
'* 13-OCT-11
'*
'* Copyright 2011 Archie Jacobs
'*
'* Implements driver for communication to ModbusTCP devices
'* 5-MAR-12 Fixed a bug where ReadAny would call itself instead of the overload
'* 9-JAN-13 When TNS was over 255 it would go out of bounds in Transactions array
'* 27-JAN-13 Add the second byte that some require for writing to bits
'*******************************************************************************
Public Class ModbusTCPCom
    Inherits ModbusBase

    '* Use a shared Data Link Layer so multiple instances will not create multiple connections
    'Private Shared DLL As List(Of MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTcpDataLinkLayer)
    Private Shared DLL As System.Collections.Concurrent.ConcurrentDictionary(Of Integer, MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTcpDataLinkLayer)

    Private DLLListLock As New Object
    Private MyDLLInstance As Integer
    Private Shared NextDLLInstance As Integer
    Protected Friend EventHandlerDLLInstance As Integer

    Public Event ConnectionClosed As EventHandler


#Region "Properties"
    Private m_IPAddress As String = "0.0.0.0"   '* this is a default value
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property IPAddress() As String
        Get
            Return m_IPAddress.ToString
        End Get
        Set(ByVal value As String)
            If m_IPAddress <> value Then
                ''* If this been attached to a DLL, then remove first
                'If EventHandlerDLLInstance = (MyDLLInstance + 1) Then
                '    RemoveDLLConnection(MyDLLInstance)
                'End If

                m_IPAddress = value

                If Not Me.DesignMode Then
                    '* If a new instance needs to be created, such as a different AMS Address
                    CreateDLLInstance()
                End If
            End If
        End Set
    End Property

    Private m_TcpipPort As UInt16 = 502
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property TcpipPort() As UInt16
        Get
            Return m_TcpipPort
        End Get
        Set(ByVal value As UInt16)
            If m_TcpipPort <> value Then
                '* If this been attached to a DLL, then remove first
                'If EventHandlerDLLInstance = (MyDLLInstance + 1) Then
                '    RemoveDLLConnection()
                'End If

                m_TcpipPort = value
                If Not Me.DesignMode Then
                    '* If a new instance needs to be created, such as a different AMS Address
                    CreateDLLInstance()
                End If
            End If
        End Set
    End Property

    Private m_UnitId As Byte
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property UnitId() As Byte
        Get
            Return m_UnitId
        End Get
        Set(ByVal value As Byte)
            If m_UnitId <> value Then
                m_UnitId = value

                If EventHandlerDLLInstance > 0 AndAlso DLL.ContainsKey(MyDLLInstance) Then
                    DLL(MyDLLInstance).UnitId = value
                End If
            End If
        End Set
    End Property
#End Region

#Region "ConstructorDestructor"
    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        MyClass.New()

        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)
    End Sub

    Public Sub New()
        MyBase.New()

        '* Default UnitID
        m_UnitId = 1

        If DLL Is Nothing Then
            DLL = New System.Collections.Concurrent.ConcurrentDictionary(Of Integer, MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTcpDataLinkLayer)
        End If
    End Sub

    'Component overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        RemoveDLLConnection(MyDLLInstance)

        MyBase.Dispose(disposing)
    End Sub

    '***************************************************************
    '* Create the Data Link Layer Instances
    '* if the IP Address is the same, then resuse a common instance
    '***************************************************************
    Private CreateDLLLockObject As New Object
    Protected Overrides Sub CreateDLLInstance()
        '* Still default, so ignore
        If m_IPAddress = "0.0.0.0" Then Exit Sub

        SyncLock (CreateDLLLockObject)
            '* Check to see if it has the same IP address and Port
            '* if so, reuse the instance, otherwise create a new one
            Dim KeyFound As Boolean
            For Each d In DLL
                If d.Value IsNot Nothing Then
                    If (d.Value.IPAddress = m_IPAddress And d.Value.Port = m_TcpipPort) Then
                        MyDLLInstance = d.Key
                        KeyFound = True
                        Exit For
                    End If
                End If
            Next

            '* A DLL instance for this IP does not exist
            If Not KeyFound Then
                NextDLLInstance += 1
                MyDLLInstance = NextDLLInstance
            End If

            '* Do we need to create a new DLL instance?
            If (Not DLL.ContainsKey(MyDLLInstance) OrElse (DLL(MyDLLInstance) Is Nothing)) Then
                Dim NewDLL As New MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTcpDataLinkLayer(m_IPAddress, m_TcpipPort)
                NewDLL.UnitId = m_UnitId
                DLL(MyDLLInstance) = NewDLL
            End If


            '* Have we already attached event handler to this data link layer?
            If EventHandlerDLLInstance <> (MyDLLInstance + 1) Then
                '* If event handler to another layer has been created, remove them
                If EventHandlerDLLInstance > 0 Then
                    If DLL.ContainsKey(EventHandlerDLLInstance - 1) Then
                        RemoveDLLConnection(EventHandlerDLLInstance - 1)
                    End If
                End If

                AddHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayerDataReceived
                AddHandler DLL(MyDLLInstance).ComError, AddressOf DataLinkLayerComError
                AddHandler DLL(MyDLLInstance).ConnectionClosed, AddressOf DLLConnectionClosed
                AddHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf DataLinkLayerConnectionEstablished
                '* Track how many instanced use this DLL, so we know when to dispose
                DLL(MyDLLInstance).ConnectionCount += 1
                EventHandlerDLLInstance = MyDLLInstance + 1
            End If
            'End If
        End SyncLock
    End Sub

    Private Sub RemoveDLLConnection(ByVal instance As Integer)
        '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
        If DLL.ContainsKey(instance) AndAlso DLL(instance) IsNot Nothing Then
            RemoveHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayerDataReceived
            RemoveHandler DLL(instance).ComError, AddressOf DataLinkLayerComError
            RemoveHandler DLL(MyDLLInstance).ConnectionClosed, AddressOf DLLConnectionClosed
            RemoveHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf DataLinkLayerConnectionEstablished
            EventHandlerDLLInstance = 0

            DLL(MyDLLInstance).ConnectionCount -= 1

            If DLL(instance).ConnectionCount <= 0 Then
                DLL(instance).Dispose()
                DLL(instance) = Nothing
                Dim x As MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTcpDataLinkLayer = Nothing
                DLL.TryRemove(instance, x)
            End If
        End If
    End Sub
#End Region

#Region "Private Methods"
    Friend Overrides Function SendRequest(ByVal PDU As ModbusPDUFrame) As Integer
        If IsDisposed Then
            Throw New ObjectDisposedException("ModbusTCPCom")
        End If

        Dim TCPFrame As MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTCPFrame
        TCPFrame = New MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTCPFrame(PDU, MyObjectID)
        TCPFrame.UnitID = m_UnitId

        'If (DLL(MyDLLInstance).SendQueDepth < 50) Then
        If DLL(MyDLLInstance) IsNot Nothing Then
            Return DLL(MyDLLInstance).SendData(TCPFrame)
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("ModbusTCP SendData DLL Instance not created")
        End If
     End Function

    Private NextTNS As Integer
    Private TIDLock As New Object
    Protected Overrides Function GetNextTransactionID(ByVal maxValue As Integer) As Integer
        SyncLock (TIDLock)
            If DLL.ContainsKey(MyDLLInstance) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                'SyncLock (DLLListLock)
                NextTNS += 1
                If NextTNS > 255 Then NextTNS = 0
                'Dim ID As Integer = DLL(MyDLLInstance).GetNextTransactionNumber(255)
                Return CInt(NextTNS + ((MyObjectID And 255) * 256))
                'End SyncLock
            Else
                Return 0
            End If
        End SyncLock
    End Function

    Protected Overrides Function IsInQue(transactionNumber As Integer, ownerObjectID As Long) As Boolean
        Return False
        'Return DLL(MyDLLInstance).IsInQue(transactionNumber, ownerObjectID)
    End Function
#End Region

#Region "Events"
    '************************************************
    '* Process data recieved from controller
    '************************************************
    Private Sub DataLinkLayerDataReceived(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        Dim TCP As New MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTCPFrame(New List(Of Byte)(e.RawData).ToArray, e.RawData.Length)

        ProcessDataReceived(TCP.PDU, e)
    End Sub

    Private Sub DLLConnectionClosed(ByVal sender As Object, ByVal e As System.EventArgs)
        OnConnectionClosed(e)
    End Sub

    Protected Overridable Sub OnConnectionClosed(ByVal e As System.EventArgs)
        If m_SynchronizingObject IsNot Nothing Then
            Dim Parameters() As Object = {Me, e}
            If DirectCast(m_SynchronizingObject, System.Windows.Forms.Control).IsHandleCreated Then
                m_SynchronizingObject.BeginInvoke(ConnectionClosedSD, Parameters)
            End If
        Else
            RaiseEvent ConnectionClosed(Me, e)
        End If
    End Sub

    Private ConnectionClosedSD As New EventHandler(AddressOf ConnectionClosedSync)
    Private Sub ConnectionClosedSync(ByVal sender As Object, ByVal e As System.EventArgs)
        RaiseEvent ConnectionClosed(Me, e)
    End Sub

#End Region

End Class