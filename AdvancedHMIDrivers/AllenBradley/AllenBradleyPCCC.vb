'**********************************************************************************************
'* AdvancedHMI Driver
'* http://www.advancedhmi.com
'* PCCC Application Layer
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* support@advancedhmi.com
'* 22-NOV-06, 03-MAR-15
'*
'* Copyright 2006, 2010, 2015 Archie Jacobs
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
'* 04-MAR-15 Rewritten to separate PCCC direct commands and higher level (e.g.Read)
'*******************************************************************************************************

'<Assembly: system.Security.Permissions.SecurityPermissionAttribute(system.Security.Permissions.SecurityAction.RequestMinimum)> 
'<Assembly: CLSCompliant(True)> 
Public MustInherit Class AllenBradleyPCCC
    Inherits System.ComponentModel.Component

    Private Shared ObjectIDs As Int64
    Protected MyObjectID As Int64

    Protected RawResponses(255) As MfgControl.AdvancedHMI.Drivers.PCCCReplyPacket
    Protected RawRequests(255) As MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket

    Protected Event ResponseReceived As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Public Event UnsolictedMessageRcvd As EventHandler
    Public Event ConnectionEstablished As EventHandler

    '**************************************************************
    '* This method implements the common application routine
    '* as discussed in the Software Layer section of the AB manual
    '**************************************************************
    Friend MustOverride Function GetNextTNSNumber() As Integer
    Friend MustOverride Sub SendPacket(packet As MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket, ByVal TNS As Integer)


#Region "Constructor"
    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        MyClass.New()

        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)
    End Sub

    Public Sub New()
        MyBase.New()

        ObjectIDs += 1
        MyObjectID = ObjectIDs
    End Sub

    Protected MustOverride Sub CreateDLLInstance()


    'Component overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        '     RemoveDLLConnection()

        MyBase.Dispose(disposing)
    End Sub
#End Region

#Region "Properties"

#End Region


#Region "PCCCCommands"
    '* Reference Page 7-5
    Public Enum Modes
        Program
        Run
    End Enum
    Public Function ChangeMode(ByVal mode As Modes, ByVal TNS As Integer) As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &H80

        pck.TransactionNumber = TNS

        '* Modes
        '* 01 = PROGRAM
        '* 06 = RUN
        If mode = Modes.Program Then
            pck.EncapsulatedData.Add(1)
        Else
            pck.EncapsulatedData.Add(6)
        End If

        SendPacket(pck, TNS)
        Return TNS
    End Function


    '* Reference Page 7-6
    Public Function DiagnosticStatus(ByVal TNS As Integer) As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = 6
        pck.FunctionCode = 3

        pck.TransactionNumber = TNS

        SendPacket(pck, TNS)
        Return TNS
    End Function

    '* Reference : Page 7-16
    Public Function ProtectedTypedFileRead(ByVal size As Byte, ByVal tag As Integer, ByVal offset As Integer, ByVal filetype As Byte, ByVal TNS As Integer) As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &HA7

        pck.TransactionNumber = TNS
        pck.EncapsulatedData.Add(size)

        Dim b() As Byte = BitConverter.GetBytes(tag)
        For i = 0 To b.Length - 1
            pck.EncapsulatedData.Add(b(i))
        Next

        b = BitConverter.GetBytes(offset)
        For i = 0 To b.Length - 1
            pck.EncapsulatedData.Add(b(i))
        Next

        pck.EncapsulatedData.Add(filetype)

        SendPacket(pck, TNS)
        Return TNS
    End Function

    '* Protected typed logical read with three address fields 
    '* Reference : Page 7-17
    Public Function ProtectedTypeLogicalRead(ByVal byteSize As Byte, ByVal fileNumber As Byte, ByVal fileType As Byte, ByVal elementNumber As Byte, ByVal subElementNumber As Byte, ByVal TNS As Integer) As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &HA2

        'Dim TNS As Integer = GetNextTNSNumber()
        pck.TransactionNumber = TNS

        pck.EncapsulatedData.Add(byteSize)
        pck.EncapsulatedData.Add(fileNumber)
        pck.EncapsulatedData.Add(fileType)
        pck.EncapsulatedData.Add(elementNumber)
        pck.EncapsulatedData.Add(subElementNumber)

        SendPacket(pck, TNS)
        Return TNS
    End Function

    '* Protected typed logical write with three address fields 
    '* Reference : Page 7-18
    Public Function ProtectedTypeLogicalWrite(ByVal byteSize As Byte, ByVal fileNumber As Byte, ByVal fileType As Byte, ByVal elementNumber As Byte, ByVal subElementNumber As Byte, ByVal data() As Byte, ByVal TNS As Integer) As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &HAA

        'Dim TNS As Integer = GetNextTNSNumber()
        pck.TransactionNumber = TNS

        pck.EncapsulatedData.Add(byteSize)
        pck.EncapsulatedData.Add(fileNumber)
        pck.EncapsulatedData.Add(fileType)
        pck.EncapsulatedData.Add(elementNumber)
        pck.EncapsulatedData.Add(subElementNumber)

        For i = 0 To byteSize - 1
            If i < data.Length Then
                pck.EncapsulatedData.Add(data(i))
            Else
                pck.EncapsulatedData.Add(0)
            End If
        Next

        SendPacket(pck, TNS)
        Return TNS
    End Function

    '* Typed Read (Word Range)
    '* Reference : Page 7-28
    '* TODO : complete this for reading from PLC5, different with WordRangeRead?
    Public Function TypedRead(ByVal address As MfgControl.AdvancedHMI.Drivers.PCCCAddress, ByVal TNS As Integer) As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &H68


        pck.TransactionNumber = TNS

        For Each val As Byte In address.ByteStream
            pck.EncapsulatedData.Add(val)
        Next

        '* Add the size
        Dim b() As Byte = BitConverter.GetBytes(CInt(address.ByteSize))
        For i = 0 To b.Length - 1
            pck.EncapsulatedData.Add(b(i))
        Next

        SendPacket(pck, TNS)
        Return TNS
    End Function

    '* Protected typed logical write with three address fields 
    '* Reference : http://www.iatips.com/pccc_tips.html
    Public Function ProtectedTypeLogicalMaskedBitWrite(ByVal byteSize As Byte, ByVal fileNumber As Byte, ByVal fileType As Byte, ByVal elementNumber As Byte, _
                                                       ByVal subElementNumber As Byte, ByVal data() As Byte, ByVal TNS As Integer) As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &HAB

        'Dim TNS As Integer = GetNextTNSNumber()
        pck.TransactionNumber = TNS

        pck.EncapsulatedData.Add(byteSize)
        pck.EncapsulatedData.Add(fileNumber)
        pck.EncapsulatedData.Add(fileType)
        pck.EncapsulatedData.Add(elementNumber)
        pck.EncapsulatedData.Add(subElementNumber)

        For i = 0 To (byteSize * 2) - 1
            If i < data.Length Then
                pck.EncapsulatedData.Add(data(i))
            Else
                pck.EncapsulatedData.Add(0)
            End If
        Next

        SendPacket(pck, TNS)
        Return TNS
    End Function


    '* Reference Page 7-34
    Public Function WordRangeRead(ByVal address As MfgControl.AdvancedHMI.Drivers.PCCCAddress, ByVal TNS As Integer) As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &H1


        'Dim TNS As Integer = GetNextTNSNumber()
        pck.TransactionNumber = TNS

        'This is basically what happened in version 397e, except the command, function and TNS were manually packed.
        For Each val As Byte In address.ByteStream
            pck.EncapsulatedData.Add(val)
        Next

        '* Add the number of bytes
        pck.EncapsulatedData.Add(CByte(address.ByteSize And 255))

        SendPacket(pck, TNS)
        Return TNS
    End Function


    '* Reference Page 7-35
    Public Function WordRangeWrite(ByVal address As MfgControl.AdvancedHMI.Drivers.PCCCAddress, ByVal data() As Byte, ByVal TNS As Integer) As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &H0


        'Dim TNS As Integer = GetNextTNSNumber()
        pck.TransactionNumber = TNS

        'This is basically what happened in version 397e, except the command, function and TNS were manually packed.
        For Each val As Byte In address.ByteStream
            pck.EncapsulatedData.Add(val)
        Next


        For index = 0 To data.Length - 1
            pck.EncapsulatedData.Add(data(index))
        Next


        SendPacket(pck, TNS)
        Return TNS
    End Function



 
    Public Function ClearFault() As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &HAB

        Dim data() As Byte = {&H2, &H2, &H84, &H5, &H0, &HFF, &HFC, &H0, &H0}
        For i = 0 To data.Length - 1
            pck.EncapsulatedData.Add(data(i))
        Next

        Dim TNS As Integer = GetNextTNSNumber()
        pck.TransactionNumber = TNS

        SendPacket(pck, TNS)
        Return TNS
    End Function

    '* Reference Page 7-7
    Public Function DownloadCompleted() As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &H52

        Dim TNS As Integer = GetNextTNSNumber()
        pck.TransactionNumber = TNS

        SendPacket(pck, TNS)
        Return TNS
    End Function


    '* Reference Page 7-11
    Public Function GetEditResource() As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &H11

        Dim TNS As Integer = GetNextTNSNumber()
        pck.TransactionNumber = TNS

        SendPacket(pck, TNS)
        Return TNS
    End Function

    '* Reference Page 7-24
    Public Function ReturnEditResource() As Integer
        Dim pck As New MfgControl.AdvancedHMI.Drivers.PCCCCommandPacket()
        pck.Command = &HF
        pck.FunctionCode = &H12

        Dim TNS As Integer = GetNextTNSNumber()
        pck.TransactionNumber = TNS

        SendPacket(pck, TNS)
        Return TNS
    End Function
#End Region

#Region "Helper"
    '**************************************************************
    '* This method Sends a response from an unsolicited msg
    '**************************************************************
    'Private Function SendResponse(ByVal Command As Byte, ByVal rTNS As Integer) As Integer
    '    Dim PacketSize As Integer
    '    'PacketSize = Data.Length + 5
    '    PacketSize = 5
    '    PacketSize = 3    'Ethernet/IP Preparation


    '    Dim CommandPacke(PacketSize) As Byte
    '    Dim BytePos As Integer

    '    CommandPacke(1) = m_TargetNode
    '    CommandPacke(0) = m_MyNode
    '    BytePos = 2
    '    BytePos = 0

    '    CommandPacke(BytePos) = Command
    '    CommandPacke(BytePos + 1) = 0       '* STS (status, always 0)

    '    CommandPacke(BytePos + 2) = (rTNS And 255)
    '    CommandPacke(BytePos + 3) = (rTNS >> 8)


    '************************************************
    '* Convert the message code number into a string
    '* Ref Page 8-3
    '************************************************
    Public Shared Function DecodeMessage(ByVal msgNumber As Integer) As String
        Select Case msgNumber
            Case 0
                DecodeMessage = ""
            Case -2
                Return "Not Acknowledged (NAK)"
            Case -3
                Return "No Reponse, Check COM Settings"
            Case -4
                Return "Unknown Message from DataLink Layer"
            Case -5
                Return "Invalid Address"
            Case -6
                Return "Could Not Open Com Port"
            Case -7
                Return "No data specified to data link layer"
            Case -8
                Return "No data returned from PLC"
            Case -9
                Return "Failed To Open COM Port " '& DLL(MyDLLInstance).ComPort
            Case -20
                Return "No Data Returned"
            Case -21
                Return "Received Message NAKd from invalid checksum"

                '*** Errors coming from PLC
            Case 16
                Return "Illegal Command or Format, Address may not exist or not enough elements in data file"
            Case 32
                Return "PLC Has a Problem and Will Not Communicate"
            Case 48
                Return "Remote Node Host is Misssing, Disconnected, or Shut Down"
            Case 64
                Return "Host Could Not Complete Function Due To Hardware Fault"
            Case 80
                Return "Addressing problem or Memory Protect Rungs"
            Case 96
                Return "Function not allows due to command protection selection"
            Case 112
                Return "Processor is in Program mode"
            Case 128
                Return "Compatibility mode file missing or communication zone problem"
            Case 144
                Return "Remote node cannot buffer command"
            Case 240
                Return "Error code in EXT STS Byte"

                '* EXT STS Section - 256 is added to code to distinguish EXT codes
            Case 257
                Return "A field has an illegal value"
            Case 258
                Return "Less levels specified in address than minimum for any address"
            Case 259
                Return "More levels specified in address than system supports"
            Case 260
                Return "Symbol not found"
            Case 261
                Return "Symbol is of improper format"
            Case 262
                Return "Address doesn't point to something usable"
            Case 263
                Return "File is wrong size"
            Case 264
                Return "Cannot complete request, situation has changed since the start of the command"
            Case 265
                Return "Data or file is too large"
            Case 266
                Return "Transaction size plus word address is too large"
            Case 267
                Return "Access denied, improper priviledge"
            Case 268
                Return "Condition cannot be generated - resource is not available"
            Case 269
                Return "Condition already exists - resource is already available"
            Case 270
                Return "Command cannot be executed"

            Case Else
                Return "Unknown Message - " & msgNumber
        End Select
    End Function


    Friend Sub DataLinkLayer_DataReceived(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If (e Is Nothing) Then Exit Sub

        Dim TNSLowerByte As Integer = e.TransactionNumber And 255

        If RawRequests(TNSLowerByte) Is Nothing Then
            Exit Sub
        End If

        If e.RawData IsNot Nothing Then
            RawResponses(TNSLowerByte) = New MfgControl.AdvancedHMI.Drivers.PCCCReplyPacket(e.RawData)
        End If

        RawRequests(TNSLowerByte).Responded = True

        OnResponseReceived(e)
    End Sub

    '******************************************************************
    '* This is called when a message instruction was sent from the PLC
    '******************************************************************
    Private Sub DF1DataLink1_UnsolictedMessageRcvd()
        RaiseEvent UnsolictedMessageRcvd(Me, System.EventArgs.Empty)
    End Sub

    Protected Overridable Sub OnResponseReceived(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        RaiseEvent ResponseReceived(Me, e)
    End Sub


    ''****************************************************************************
    ''* This is required to sync the event back to the parent form's main thread
    ''****************************************************************************
    'Private drsd As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs) = AddressOf DataReceivedSync
    ''Private Sub DataReceivedSync(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    'Private Sub DataReceivedSync(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    '    RaiseEvent DataReceived(sender, e)
    'End Sub
    Private Sub UnsolictedMessageRcvdSync(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent UnsolictedMessageRcvd(sender, e)
    End Sub

    Protected Sub CIPConnectionEstablished(ByVal sender As Object, e As EventArgs)
        RaiseEvent ConnectionEstablished(Me, e)
    End Sub
#End Region


    '****************************************************
    '* Wait for a response from PLC before returning
    '****************************************************
    Private MaxTicks As Integer = 750  '* 50 ticks per second
    Private Function WaitForResponse(ByVal rTNS As Integer) As Integer
        Dim Loops As Integer = 0
        Dim TNSLowerByte As Integer = rTNS And 255
        While Not RawRequests(TNSLowerByte).Responded And Loops < MaxTicks
            System.Threading.Thread.Sleep(2)
            Loops += 1
        End While


        If Loops >= MaxTicks Then
            Return -20
            'ElseIf DLL(MyDLLInstance).LastResponseWasNAK Then
            '    Return -21
        End If

        Return 0
    End Function

End Class

