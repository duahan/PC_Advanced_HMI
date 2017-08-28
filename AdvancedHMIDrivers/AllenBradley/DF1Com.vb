'**********************************************************************************************
'* AdvancedHMI Driver
'* http://www.advancedhmi.com
'* DF1 Data Link Layer & Application Layer
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* support@advancedhmi.com
'* 22-NOV-06
'*
'* Copyright 2006, 2010 Archie Jacobs
'*
'* This class implements the two layers of the Allen Bradley DF1 protocol.
'* In terms of the AB documentation, the data link layer acts as the transmitter and receiver.
'* Communication commands in the format described in chapter 7, are passed to
'* the data link layer using the SendData method.
'*
'* Reference : Allen Bradley Publication 1770-6.5.16
'*
'* Distributed under the GNU General Public License (www.gnu.org)
'*
'* This program is free software; you can redistribute it and/or
'* as published by the Free Software Foundation; either version 2
'* of the License, or (at your option) any later version.
'*
'* This program is distributed in the hope that it will be useful,
'* but WITHOUT ANY WARRANTY; without even the implied warranty of
'* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'* GNU General Public License for more details.

'* You should have received a copy of the GNU General Public License
'* along with this program; if not, write to the Free Software
'* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
'*
'*
'* 09-JUL-11  Split up in order to make a single class common for both DF1 and EthernetIP
'*******************************************************************************************************
Imports System.ComponentModel.Design
Imports System.ComponentModel

'<Assembly: system.Security.Permissions.SecurityPermissionAttribute(system.Security.Permissions.SecurityAction.RequestMinimum)> 
Public Class DF1Com
    Inherits AllenBradleySLCMicro
    Implements MfgControl.AdvancedHMI.Drivers.IComComponent

    '* Create a common instance to share so multiple DF1Comms can be used in a project
    Private Shared DLL As System.Collections.Concurrent.ConcurrentDictionary(Of Integer, MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayerR1)
    Private MyDLLInstance As Integer
    Private Shared NextDLLInstance As Integer
    Protected Friend EventHandlerDLLInstance As Integer

    Public Event AutoDetectTry As EventHandler


#Region "Constructor"
    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        Me.new()

        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)
    End Sub

    Public Sub New()
        MyBase.New()

        If DLL Is Nothing Then
            DLL = New System.Collections.Concurrent.ConcurrentDictionary(Of Integer, MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayerR1)
        End If
    End Sub

    '***************************************************************
    '* Create the Data Link Layer Instances
    '* if the IP Address is the same, then resuse a common instance
    '***************************************************************
    Private CreateDLLLockObject As New Object
    Protected Overrides Sub CreateDLLInstance()
        '*** For Windows CE port, this checks designmode and works in full .NET also***
        If AppDomain.CurrentDomain.FriendlyName.IndexOf("DefaultDomain", System.StringComparison.CurrentCultureIgnoreCase) >= 0 Then
            Exit Sub
        End If

        SyncLock (CreateDLLLockObject)
            '* Check to see if it has the same IP address and Port
            '* if so, reuse the instance, otherwise create a new one
            Dim KeyFound As Boolean
            For Each d In DLL
                If d.Value IsNot Nothing Then
                    If (d.Value.ComPort = m_ComPort) Then
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
                Dim NewDLL As New MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayerR1
                If String.Compare(m_BaudRate, "AUTO", True) <> 0 Then
                    NewDLL.BaudRate = Convert.ToInt32(m_BaudRate)
                End If
                NewDLL.Parity = m_Parity
                NewDLL.ChecksumType = m_CheckSumType
                NewDLL.ComPort = m_ComPort
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


                AddHandler DLL(MyDLLInstance).DataReceived, AddressOf Df1DataLinkLayerDataReceived
                AddHandler DLL(MyDLLInstance).ComError, AddressOf Df1DataLinkLayerComError
                'AddHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf DataLinkLayerConnectionEstablished
                DLL(MyDLLInstance).ConnectionCount += 1
                EventHandlerDLLInstance = MyDLLInstance + 1
            End If
        End SyncLock
    End Sub

    Private Sub RemoveDLLConnection(ByVal instance As Integer)
        '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
        If DLL.ContainsKey(instance) AndAlso DLL(instance) IsNot Nothing Then
            RemoveHandler DLL(instance).DataReceived, AddressOf Df1DataLinkLayerDataReceived
            RemoveHandler DLL(instance).ComError, AddressOf Df1DataLinkLayerComError
            'RemoveHandler DLL(instance).ConnectionEstablished, AddressOf DataLinkLayerConnectionEstablished
            EventHandlerDLLInstance = 0

            DLL(instance).ConnectionCount -= 1

            If DLL(instance).ConnectionCount <= 0 Then
                DLL(instance).Dispose()
                DLL(instance) = Nothing
                Dim x As MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayerR1 = Nothing
                DLL.TryRemove(instance, x)
            End If
        End If
    End Sub



    'Component overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        RemoveDLLConnection(MyDLLInstance)

        MyBase.Dispose(disposing)
    End Sub
#End Region

#Region "Properties"
    Private m_BaudRate As String = "AUTO"
    <EditorAttribute(GetType(BaudRateEditor), GetType(System.Drawing.Design.UITypeEditor))> _
    Public Property BaudRate() As String
        Get
            Return m_BaudRate
        End Get
        Set(ByVal value As String)
            If value <> m_BaudRate Then
                If Not Me.DesignMode Then
                    '* If a new instance needs to be created, such as a different Com Port
                    CreateDLLInstance()

                    If DLL IsNot Nothing Then
                        If DLL.Count >= MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                            DLL(MyDLLInstance).CloseCom()
                            Try
                                DLL(MyDLLInstance).BaudRate = Convert.ToInt32(value)
                            Catch ex As Exception
                                '* 0 means AUTO to the data link layer
                                DLL(MyDLLInstance).BaudRate = 0
                            End Try
                        End If
                    End If
                End If
                m_BaudRate = value
            End If
        End Set
    End Property

    '* This is need so the current value of Auto detect can be viewed
    Public ReadOnly Property ActualBaudRate() As Integer
        Get
            If DLL.Count <= 0 OrElse DLL(MyDLLInstance) Is Nothing Then
                Return 0
            Else
                Return DLL(MyDLLInstance).BaudRate
            End If
        End Get
    End Property

    Private m_ComPort As String = "COM1"
    Public Property ComPort() As String
        Get
            'Return DLL(MyDLLInstance).ComPort
            Return m_ComPort
        End Get
        Set(ByVal value As String)
            'If value <> DLL(MyDLLInstance).ComPort Then DLL(MyDLLInstance).CloseComms()
            'DLL(MyDLLInstance).ComPort = value
            m_ComPort = value

            '* If a new instance needs to be created, such as a different Com Port
            'CreateDLLInstance()


            If MyDLLInstance > 0 AndAlso (DLL IsNot Nothing) AndAlso (DLL.ContainsKey(MyDLLInstance)) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                'Else
                DLL(MyDLLInstance).ComPort = value
            End If
        End Set
    End Property

    Private m_Parity As System.IO.Ports.Parity = IO.Ports.Parity.None
    Public Property Parity() As System.IO.Ports.Parity
        Get
            Return m_Parity
        End Get
        Set(ByVal value As System.IO.Ports.Parity)
            m_Parity = value
        End Set
    End Property


    Private m_CheckSumType As MfgControl.AdvancedHMI.Drivers.DF1Transport.ChecksumOptions
    Public Property CheckSumType() As MfgControl.AdvancedHMI.Drivers.DF1Transport.ChecksumOptions
        Get
            Return m_CheckSumType
        End Get
        Set(ByVal value As MfgControl.AdvancedHMI.Drivers.DF1Transport.ChecksumOptions)
            m_CheckSumType = value
            If MyDLLInstance > 0 AndAlso (DLL.ContainsKey(MyDLLInstance)) AndAlso DLL(MyDLLInstance) IsNot Nothing Then   'AndAlso Not DLL(MyDLLInstance).IsPortOpen Then
                DLL(MyDLLInstance).ChecksumType = m_CheckSumType
            End If
        End Set
    End Property

    Protected m_MyNode As Byte
    Public Property MyNode() As Integer
        Get
            Return m_MyNode
        End Get
        Set(ByVal value As Integer)
            m_MyNode = CByte(value And 255)
        End Set
    End Property

    Protected m_TargetNode As Byte
    Public Property TargetNode() As Integer
        Get
            Return m_TargetNode
        End Get
        Set(ByVal value As Integer)
            m_TargetNode = CByte(value And 255)
        End Set
    End Property


    '**************************************************
    '* Its purpose is to fetch
    '* the main form in order to synchronize the
    '* notification thread/event
    '**************************************************
    Protected m_SynchronizingObject As System.ComponentModel.ISynchronizeInvoke
    '* do not let this property show up in the property window
    ' <System.ComponentModel.Browsable(False)> _
    Public Overrides Property SynchronizingObject() As System.ComponentModel.ISynchronizeInvoke
        Get
            'If Me.Site.DesignMode Then

            Dim host1 As IDesignerHost
            Dim obj1 As Object
            If (m_SynchronizingObject Is Nothing) AndAlso Me.DesignMode Then
                host1 = CType(Me.GetService(GetType(IDesignerHost)), IDesignerHost)
                If host1 IsNot Nothing Then
                    obj1 = host1.RootComponent
                    m_SynchronizingObject = CType(obj1, System.ComponentModel.ISynchronizeInvoke)
                End If
            End If
            'End If
            Return m_SynchronizingObject


        End Get

        Set(ByVal Value As System.ComponentModel.ISynchronizeInvoke)
            If Value IsNot Nothing Then
                m_SynchronizingObject = Value
            End If
        End Set
    End Property
#End Region

#Region "Public Methods"

#End Region

#Region "Helper"
    Private Sub Df1DataLinkLayerDataReceived(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If e.ErrorId = 0 Then
            '* Remove the nodes then send to AllenBradleyPCCC
            Dim DataWithNoNodes(e.RawData.Length - 3) As Byte
            Array.ConstrainedCopy(e.RawData, 2, DataWithNoNodes, 0, DataWithNoNodes.Length)
            Dim e1 As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(DataWithNoNodes, "", e.TransactionNumber, e.OwnerObjectID)

            DataLinkLayer_DataReceived(sender, e1)
        Else '
            DataLinkLayer_DataReceived(sender, e)
            Dim dbg = 0
        End If
    End Sub

    Private Sub Df1DataLinkLayerComError(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        waitHandle(e.TransactionNumber And 255).Set()
        Dim dbg = 0
    End Sub

    Friend Overrides Sub SendPacket(packet As MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket, ByVal TNS As Integer)
        Dim TNSByte As Byte = Convert.ToByte(TNS And 255)
        RawRequests(TNSByte) = packet
        RawRequests(TNSByte).Responded = False

        Dim data() As Byte = packet.GetBytes
        Dim dataWithNodes(data.Length + 1) As Byte
        data.CopyTo(dataWithNodes, 2)
        dataWithNodes(0) = m_TargetNode
        dataWithNodes(1) = m_MyNode

        DLL(MyDLLInstance).SendData(dataWithNodes, TNS, MyObjectID)
    End Sub


    '**************************************************************
    '* This method Sends a response from an unsolicited msg
    '**************************************************************
    Private Function SendResponse(ByVal Command As Byte, ByVal rTNS As Integer) As Integer
        Dim PacketSize As Integer
        'PacketSize = Data.Length + 5
        PacketSize = 5
        PacketSize = 3    'Ethernet/IP Preparation


        Dim CommandPacket(PacketSize) As Byte
        Dim BytePos As Integer

        CommandPacket(1) = CByte(TargetNode And 255)
        CommandPacket(0) = CByte(MyNode And 255)
        BytePos = 2
        BytePos = 0

        CommandPacket(BytePos) = Command
        CommandPacket(BytePos + 1) = 0       '* STS (status, always 0)

        CommandPacket(BytePos + 2) = CByte(rTNS And 255)
        CommandPacket(BytePos + 3) = CByte(rTNS >> 8)


        Dim result As Integer
        result = DLL(MyDLLInstance).SendData(CommandPacket, rTNS, MyObjectID)
    End Function




    Friend Overrides Function GetNextTNSNumber() As Integer
        If MyDLLInstance > 0 AndAlso DLL IsNot Nothing AndAlso DLL.Count >= MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
        Else
            CreateDLLInstance()
        End If
        Return DLL(MyDLLInstance).GetNextTransactionNumber(32767)
    End Function
#End Region
End Class

