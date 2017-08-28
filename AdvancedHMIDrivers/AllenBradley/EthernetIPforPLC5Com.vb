'**********************************************************************************************
'* AdvancedHMI Driver
'* http://www.advancedhmi.com
'* PLC5 PCCC over Ethernet/IP
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* support@advancedhmi.com
'* 03-SEP-15
'*
'* Copyright 2015 Archie Jacobs
'*
'* NOTICE : If you received this code without a complete AdvancedHMI solution
'* please report to sales@advancedhmi.com
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
'*******************************************************************************************************

Public Class EthernetIPforPLC5Com
    Inherits AllenBradleyPLC5
    Implements MfgControl.AdvancedHMI.Drivers.IComComponent

    '* Create a common instance to share so multiple DF1Comms can be used in a project
    Private Shared DLL As List(Of MfgControl.AdvancedHMI.Drivers.CIPforPCCC)
    Private MyDLLInstance As Integer
    Protected Friend EventHandlerDLLInstance As Integer

    Public Event IPAddressChanged As EventHandler

#Region "Constructor"
    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        MyClass.New()

        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)
    End Sub

    Public Sub New()
        MyBase.New()

        If DLL Is Nothing Then
            DLL = New List(Of MfgControl.AdvancedHMI.Drivers.CIPforPCCC)
        End If

        m_CIPConnectionSize = 508
    End Sub

    'Component overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
        If DLL.Count > MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
            RemoveDLLConnection()
        End If

        MyBase.Dispose(disposing)
    End Sub

    '***************************************************************
    '* Create the Data Link Layer Instances
    '* if the IP Address is the same, then resuse a common instance
    '***************************************************************
    Protected Overrides Sub CreateDLLInstance()
        If Not Me.DesignMode Then
            If DLL.Count > 0 Then
                '* At least one DLL instance already exists,
                '* so check to see if it has the same IP address
                '* if so, reuse the instance, otherwise create a new one
                MyDLLInstance = 0
                While MyDLLInstance < DLL.Count AndAlso (DLL(MyDLLInstance) Is Nothing OrElse _
                           (DLL(MyDLLInstance).EIPEncap.IPAddress <> m_IPAddress Or DLL(MyDLLInstance).EIPEncap.Port <> m_Port))
                    MyDLLInstance += 1
                End While
            End If

            If MyDLLInstance >= DLL.Count Then
                Dim NewDLL As New MfgControl.AdvancedHMI.Drivers.CIPforPCCC
                NewDLL.EIPEncap.IPAddress = m_IPAddress
                NewDLL.EIPEncap.Port = m_Port
                '* The SLC/Micro/ENI cannot handle the full CIP packet size
                NewDLL.ConnectionByteSize = m_CIPConnectionSize
                DLL.Add(NewDLL)
            End If

            '* Have we already attached event handler to this data link layer?
            If EventHandlerDLLInstance <> (MyDLLInstance + 1) Then
                '* If event handler to another layer has been created, remove them
                If EventHandlerDLLInstance > 0 Then
                    RemoveHandler DLL(EventHandlerDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
                    RemoveHandler DLL(EventHandlerDLLInstance).ConnectionEstablished, AddressOf CIPConnectionEstablished
                End If

                AddHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
                AddHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf CIPConnectionEstablished
                EventHandlerDLLInstance = MyDLLInstance + 1

                '* Track how many instanced use this DLL, so we know when to dispose
                DLL(MyDLLInstance).ConnectionCount += 1
            End If
        End If
    End Sub

    Private Sub RemoveDLLConnection()
        '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
        If DLL.Count > MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
            RemoveHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
            RemoveHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf CIPConnectionEstablished
            EventHandlerDLLInstance = 0

            DLL(MyDLLInstance).ConnectionCount -= 1

            If DLL(MyDLLInstance).ConnectionCount <= 0 Then
                DLL(MyDLLInstance).ForwardClose()
                DLL(MyDLLInstance).Dispose()
                DLL(MyDLLInstance) = Nothing
            End If
        End If
    End Sub
#End Region

#Region "Properties"
    Private m_IPAddress As String = "192.168.0.10"
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property IPAddress() As String
        Get
            'Return DLL(MyDLLInstance).EIPEncap.IPAddress
            Return m_IPAddress
        End Get
        Set(ByVal value As String)
            If m_IPAddress <> value Then
                '* Stop the subscriptions and allow the last request to complete
                Dim SubscriptionStatus As Boolean = Me.DisableSubscriptions
                Me.DisableSubscriptions = True
                System.Threading.Thread.Sleep(500)

                '* If this been attached to a DLL, then remove first
                If EventHandlerDLLInstance = (MyDLLInstance + 1) Then
                    RemoveDLLConnection()
                End If

                m_IPAddress = value

                If Not Me.DesignMode Then
                    '* If a new instance needs to be created, such as a different AMS Address
                    CreateDLLInstance()

                    If DLL.Count > MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                        DLL(MyDLLInstance).EIPEncap.IPAddress = value
                    End If
                End If

                '* Restart the subscriptions
                Me.DisableSubscriptions = SubscriptionStatus

                OnIPAddressChanged()
            End If
        End Set
    End Property

    Private m_Port As UShort = &HAF12
    Public Property Port As Integer
        Get
            Return m_Port
        End Get
        Set(value As Integer)
            If value <> m_Port Then
                '* If this been attached to a DLL, then remove first
                If EventHandlerDLLInstance = (MyDLLInstance + 1) Then
                    RemoveDLLConnection()
                End If

                '* Limit the value to 0-65535
                m_Port = CUShort(Math.Max(0, Math.Min(value, 65535)))
            End If
        End Set
    End Property

    '* This is the CIP connection size used in the Forward Open
    Private m_CIPConnectionSize As Integer
    Public Property CIPConnectionSize As Integer
        Get
            Return m_CIPConnectionSize
        End Get
        Set(value As Integer)
            m_CIPConnectionSize = Math.Min(value, 511)
            m_CIPConnectionSize = Math.Max(100, m_CIPConnectionSize)
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

            Dim host1 As System.ComponentModel.Design.IDesignerHost
            Dim obj1 As Object
            If (m_SynchronizingObject Is Nothing) AndAlso MyBase.DesignMode Then
                host1 = CType(Me.GetService(GetType(System.ComponentModel.Design.IDesignerHost)), System.ComponentModel.Design.IDesignerHost)
                If host1 IsNot Nothing Then
                    obj1 = host1.RootComponent
                    m_SynchronizingObject = CType(obj1, System.ComponentModel.ISynchronizeInvoke)
                End If
            End If
            'End If
            Return m_SynchronizingObject

        End Get

        Set(ByVal Value As System.ComponentModel.ISynchronizeInvoke)
            If Not Value Is Nothing Then
                m_SynchronizingObject = Value
            End If
        End Set
    End Property
#End Region

#Region "Helper"
    '****************************************************
    '* Wait for a response from PLC before returning
    '****************************************************
    'Dim MaxTicks As Integer = 500  '* 50 ticks per second
    'Private Function WaitForResponse(ByVal rTNS As Integer) As Integer
    '    'Responded = False

    '    Dim Loops As Integer = 0
    '    While Not RawRequests(rTNS And 255).Responded And Loops < MaxTicks
    '        'Application.DoEvents()
    '        System.Threading.Thread.Sleep(1)
    '        Loops += 1
    '    End While

    '    If Loops >= MaxTicks Then
    '        Return -20
    '    Else
    '        Return 0
    '    End If
    'End Function

    Friend Overrides Sub SendPacket(packet As MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket, ByVal TNS As Integer)
        ''14-OCT-12, 16-OCT-12 Return a negative value, so it knows nothing was sent
        'If m_IPAddress = "0.0.0.0" Then
        '    Return -10000
        'End If

        Dim TNSLowerByte As Byte = CByte(TNS And &HFF)
        RawRequests(TNSLowerByte) = packet
        RawRequests(TNSLowerByte).Responded = False
        Dim result As Integer
        'result = SendData(packet.GetBytes, TNS)
        result = SendData(packet, TNS)
    End Sub

    ''**************************************************************
    ''* This method implements the common application routine
    ''* as discussed in the Software Layer section of the AB manual
    ''**************************************************************
    'Friend Overrides Function PrefixAndSend(ByVal Command As Byte, ByVal Func As Byte, ByVal data() As Byte, ByVal Wait As Boolean, ByVal TNS As Integer) As Integer
    '    '14-OCT-12, 16-OCT-12 Return a negative value, so it knows nothing was sent
    '    If m_IPAddress = "0.0.0.0" Then
    '        Return -10000
    '    End If

    '    Dim PacketSize As Integer
    '    'PacketSize = data.Length + 6
    '    PacketSize = data.Length + 4 '* make this more generic for CIP Ethernet/IP encap


    '    Dim CommandPacket(PacketSize) As Byte

    '    Dim TNSLowerByte As Byte = CByte(TNS And &HFF)

    '    CommandPacket(0) = Command
    '    CommandPacket(1) = 0       '* STS (status, always 0)


    '    CommandPacket(2) = TNSLowerByte
    '    CommandPacket(3) = CByte(TNS >> 8)

    '    '*Mark whether this was requested by a subscription or not
    '    '* FIX
    '    'PLCAddressByTNS(TNSLowerByte).InternallyRequested = InternalRequest


    '    CommandPacket(4) = Func

    '    If data.Length > 0 Then
    '        data.CopyTo(CommandPacket, 5)
    '    End If

    '    RawRequests(TNSLowerByte).Responded = False
    '    Dim result As Integer
    '    result = SendData(CommandPacket, TNS)


    '    If result = 0 And Wait Then
    '        result = WaitForResponse(TNSLowerByte)

    '        '* Return status byte that came from controller
    '        If result = 0 Then
    '            If RawResponses(TNSLowerByte).EncapsulatedData IsNot Nothing Then
    '                If (RawResponses(TNSLowerByte).EncapsulatedData.Count > 3) Then
    '                    result = RawResponses(TNSLowerByte).EncapsulatedData(3)  '* STS position in DF1 message
    '                    '* If its and EXT STS, page 8-4
    '                    If result = &HF0 Then
    '                        '* The EXT STS is the last byte in the packet
    '                        'result = DataPackets(rTNS)(DataPackets(rTNS).Count - 2) + &H100
    '                        result = RawResponses(TNSLowerByte).EncapsulatedData(RawResponses(TNSLowerByte).EncapsulatedData.Count - 1) + &H100
    '                    End If
    '                End If
    '            Else
    '                result = -8 '* no response came back from PLC
    '            End If
    '        Else
    '            Dim DebugCheck As Integer = 0
    '        End If
    '    Else
    '        Dim DebugCheck As Integer = 0
    '    End If

    '    Return result
    'End Function

    '**************************************************************
    '* This method Sends a response from an unsolicited msg
    '**************************************************************
    Private Function SendResponse(ByVal Command As Byte, ByVal rTNS As Integer) As Integer
        Dim PacketSize As Integer
        'PacketSize = Data.Length + 5
        'PacketSize = 5
        PacketSize = 3    'Ethernet/IP Preparation


        Dim CommandPacket(PacketSize) As Byte
        Dim BytePos As Integer

        'CommandPacket(1) = m_TargetNode
        'CommandPacket(0) = m_MyNode
        'BytePos = 2
        BytePos = 0

        CommandPacket(BytePos) = Command
        CommandPacket(BytePos + 1) = 0       '* STS (status, always 0)

        CommandPacket(BytePos + 2) = CByte(rTNS And 255)
        CommandPacket(BytePos + 3) = CByte(rTNS >> 8)

        Dim pccc As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket
        pccc.Command = Command

        Dim result As Integer
        'result = SendData(CommandPacket, rTNS)
        result = SendData(pccc, rTNS)
    End Function

    '* This is needed so the handler can be removed
    'Private Dr As EventHandler = AddressOf DataLinkLayer_DataReceived
    'Private Function SendData(ByVal data() As Byte, ByVal MyNode As Byte, ByVal TargetNode As Byte) As Integer
    Private Function SendData(ByVal pccc As MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket, ByVal TNS As Integer) As Integer
        If DLL IsNot Nothing AndAlso DLL.Count > MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
        Else
            CreateDLLInstance()
        End If

        Return DLL(MyDLLInstance).ExecutePCCC(pccc, TNS, MyObjectID)
    End Function


    Friend Overrides Function GetNextTNSNumber() As Integer
        If DLL IsNot Nothing AndAlso DLL.Count > MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
        Else
            CreateDLLInstance()
        End If
        Return DLL(MyDLLInstance).GetNextTransactionNumber(32767)
    End Function

    Protected Overridable Sub OnIPAddressChanged()
        RaiseEvent IPAddressChanged(Me, System.EventArgs.Empty)
    End Sub
#End Region

#Region "Public Methods"
    Public Sub CloseConnection()
        RemoveDLLConnection()
        'DLL(MyDLLInstance).ForwardClose()
    End Sub
#End Region

End Class