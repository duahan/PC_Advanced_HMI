﻿Option Strict On

Imports MfgControl.AdvancedHMI.Drivers.Common
Imports MfgControl.AdvancedHMI.Drivers.ModbusTCP
Imports MfgControl.AdvancedHMI.Drivers.ModbusRTU
Imports MfgControl.AdvancedHMI.Drivers.Modbus

'***********************************************************************************
'* Modbus RTU Communication driver for AdvancedHMI
'*
'* Copyright 2012, 2014 Archie Jacobs
'*
'* This driver is developed to be used and distributed only with AdvancedHMI
'* By using this software, you agree to the GPL license and only distribute
'* with full source code and all original components that were part of AdvacnedHMI
'*
'* Reference : 
'*
'*
'* 07-MAR-12 Created
'************************************************************************************
Namespace ModbusRTU
    Public Class ModbusRTUCom
        Inherits ModbusBase

        'Private Shared DLL As List(Of ModbusRTUDataLinkLayer)
        Private Shared DLL As System.Collections.Concurrent.ConcurrentDictionary(Of Integer, MfgControl.AdvancedHMI.Drivers.ModbusRTU.ModbusRTUDataLinkLayer)


        Protected Friend MyDLLInstance As Integer
        Private Shared NextDLLInstance As Integer
        Protected Friend EventHandlerDLLInstance As Integer
        Private DLLListLock As New Object

#Region "Properties"
        Private m_PortName As String = "COM1"
        <System.ComponentModel.Category("Communication Settings")> _
        Public Property PortName() As String
            Get
                Return m_PortName
            End Get
            Set(ByVal value As String)
                m_PortName = value

                CreateDLLInstance()

                If DLL.ContainsKey(MyDLLInstance) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                    DLL(MyDLLInstance).PortName = value
                End If
            End Set
        End Property

        Private m_BaudRate As Integer = 19200
        <System.ComponentModel.Category("Communication Settings")> _
        Public Property BaudRate() As Integer
            Get
                Return m_BaudRate
            End Get
            Set(ByVal value As Integer)
                m_BaudRate = value

                CreateDLLInstance()

                If DLL.ContainsKey(MyDLLInstance) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                    DLL(MyDLLInstance).BaudRate = value
                End If
            End Set
        End Property

        Private m_Parity As System.IO.Ports.Parity = IO.Ports.Parity.None
        <System.ComponentModel.Category("Communication Settings")> _
        Public Property Parity() As System.IO.Ports.Parity
            Get
                Return m_Parity
            End Get
            Set(ByVal value As System.IO.Ports.Parity)
                m_Parity = value
                CreateDLLInstance()

                If DLL.ContainsKey(MyDLLInstance) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                    DLL(MyDLLInstance).Parity = value
                End If
            End Set
        End Property

        Private m_DataBits As Integer = 8
        <System.ComponentModel.Category("Communication Settings")> _
        Public Property DataBits() As Integer
            Get
                Return m_DataBits
            End Get
            Set(ByVal value As Integer)
                m_DataBits = value
                CreateDLLInstance()

                If DLL.ContainsKey(MyDLLInstance) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                    DLL(MyDLLInstance).DataBits = value
                End If
            End Set
        End Property

        Private m_StopBits As IO.Ports.StopBits = IO.Ports.StopBits.One
        <System.ComponentModel.Category("Communication Settings")> _
        Public Property StopBits() As IO.Ports.StopBits
            Get
                Return m_StopBits
            End Get
            Set(ByVal value As IO.Ports.StopBits)
                m_StopBits = value

                CreateDLLInstance()

                If DLL.ContainsKey(MyDLLInstance) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                    DLL(MyDLLInstance).StopBits = value
                End If
            End Set
        End Property


        Private m_StationAddress As Byte = 1
        <System.ComponentModel.Category("Communication Settings")> _
        Public Property StationAddress() As Byte
            Get
                Return m_StationAddress
            End Get
            Set(ByVal value As Byte)
                'If DLL.ContainsKey(MyDLLInstance) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                '    DLL(MyDLLInstance).StationAddress = value
                'End If
                m_StationAddress = value
            End Set
        End Property
#End Region

#Region "Constructor"
        Public Sub New()
            MyBase.new()

            If DLL Is Nothing Then
                DLL = New System.Collections.Concurrent.ConcurrentDictionary(Of Integer, MfgControl.AdvancedHMI.Drivers.ModbusRTU.ModbusRTUDataLinkLayer)
            End If
        End Sub

        Public Sub New(ByVal container As System.ComponentModel.IContainer)
            MyClass.New()

            'Required for Windows.Forms Class Composition Designer support
            container.Add(Me)
        End Sub


        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            RemoveDLLConnection(MyDLLInstance)
            MyBase.Dispose(disposing)
        End Sub

        '***************************************************************
        '* Create the Data Link Layer Instances
        '* if the COM Port is the same, then resuse a common instance
        '***************************************************************
        Private CreateDLLLockObject As New Object
        Protected Overrides Sub CreateDLLInstance()
            'If Me.DesignMode Then Exit Sub
            '*** For Windows CE port, this checks designmode and works in full .NET also***
            If AppDomain.CurrentDomain.FriendlyName.IndexOf("DefaultDomain", System.StringComparison.CurrentCultureIgnoreCase) >= 0 Then
                Exit Sub
            End If

            SyncLock (CreateDLLLockObject)
                '* Check to see if it has the same Port
                '* if so, reuse the instance, otherwise create a new one
                Dim KeyFound As Boolean
                For Each d In DLL
                    If d.Value IsNot Nothing Then
                        If (d.Value.PortName = m_PortName) Then
                            MyDLLInstance = d.Key
                            KeyFound = True
                            Exit For
                        End If
                    End If
                Next

                If Not KeyFound Then
                    NextDLLInstance += 1
                    MyDLLInstance = NextDLLInstance
                End If

                If (Not DLL.ContainsKey(MyDLLInstance) OrElse (DLL(MyDLLInstance) Is Nothing)) Then
                    Dim NewDLL As New MfgControl.AdvancedHMI.Drivers.ModbusRTU.ModbusRTUDataLinkLayer(m_PortName)
                    NewDLL.BaudRate = m_BaudRate
                    NewDLL.DataBits = m_DataBits
                    NewDLL.Parity = m_Parity
                    NewDLL.StopBits = m_StopBits
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
                    AddHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf DataLinkLayerConnectionEstablished
                    DLL(MyDLLInstance).ConnectionCount += 1
                    EventHandlerDLLInstance = MyDLLInstance + 1
                End If
            End SyncLock
        End Sub


        Private Sub RemoveDLLConnection(ByVal instance As Integer)
            '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
            If DLL.ContainsKey(instance) AndAlso DLL(instance) IsNot Nothing Then
                RemoveHandler DLL(instance).DataReceived, AddressOf DataLinkLayerDataReceived
                RemoveHandler DLL(instance).ComError, AddressOf DataLinkLayerComError
                RemoveHandler DLL(instance).ConnectionEstablished, AddressOf DataLinkLayerConnectionEstablished
                EventHandlerDLLInstance = 0

                DLL(instance).ConnectionCount -= 1

                If DLL(instance).ConnectionCount <= 0 Then
                    DLL(instance).Dispose()
                    DLL(instance) = Nothing
                    Dim x As MfgControl.AdvancedHMI.Drivers.ModbusRTU.ModbusRTUDataLinkLayer = Nothing
                    DLL.TryRemove(instance, x)
                End If
            End If
        End Sub
#End Region


#Region "Private Methods"
        Friend Overrides Function SendRequest(ByVal PDU As ModbusPDUFrame) As Integer
            If IsDisposed Then
                Throw New ObjectDisposedException("ModbusRTUCom")
            End If

            '* If a Subscription (Internal Request) begin to overflow the que, ignore some
            '* This can occur from too fast polling
            If DLL.Count <= 0 Then
                CreateDLLInstance()
            End If

            Dim RTUFrame As ModbusRTUFrame
            RTUFrame = New ModbusRTUFrame(m_StationAddress, PDU)

            If (DLL(MyDLLInstance).SendQueDepth < 50) Then
                Return DLL(MyDLLInstance).SendRTUFrame(RTUFrame, MyObjectID)
                ' Return True
            Else
                Throw New PLCDriverException("Send Que Full")
            End If
        End Function

        Protected Overrides Function GetNextTransactionID(ByVal maxValue As Integer) As Integer
            If DLL.ContainsKey(MyDLLInstance) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                Return DLL(MyDLLInstance).GetNextTransactionNumber(maxValue)
            Else
                Return 0
            End If
        End Function

        Protected Overrides Function IsInQue(transactionNumber As Integer, ownerObjectID As Long) As Boolean
            Return DLL(MyDLLInstance).IsInQue(transactionNumber, ownerObjectID)
        End Function
#End Region

#Region "Public Methods"
        Public Sub CloseConnection()
            If DLL.ContainsKey(MyDLLInstance) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                DLL(MyDLLInstance).CloseCom()
            End If
        End Sub
#End Region

#Region "Events"
        '************************************************
        '* Process data recieved from controller
        '************************************************
        Protected Sub DataLinkLayerDataReceived(ByVal sender As Object, ByVal e As PlcComEventArgs)
            '* Not enough data to make up a FINS packet
            If e.RawData Is Nothing OrElse e.RawData.Length < 4 Then
                Exit Sub
            End If

            Dim RTU As New ModbusRTUFrame(New List(Of Byte)(e.RawData).ToArray, e.RawData.Length)

            ProcessDataReceived(RTU.PDU, e)
        End Sub
#End Region

    End Class
End Namespace