'**********************************************************************************************
'* AdvancedHMI Driver for Allen Bradley PLC5 Family
'* http://www.advancedhmi.com
'* PCCC Data Link Layer & Application Layer
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* support@advancedhmi.com
'* 03-SEP-15
'*
'* Copyright 2015 Archie Jacobs
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

Public MustInherit Class AllenBradleyPLC5
    Inherits AllenBradleyPCCC
    Implements MfgControl.AdvancedHMI.Drivers.IComComponent

    Public Event DataReceived As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)


    Private Responses(255) As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs
    Friend Requests(255) As MfgControl.AdvancedHMI.Drivers.PCCCAddress


    '**************************************************************
    '* This method implements the common application routine
    '* as discussed in the Software Layer section of the AB manual
    '**************************************************************
    'Friend MustOverride Function PrefixAndSend(ByVal Command As Byte, ByVal Func As Byte, ByVal data() As Byte, ByVal Wait As Boolean, ByVal TNS As Integer) As Integer


    '* keep the original address by ref of low TNS byte so it can be returned to a linked polling address
    Private SubscriptionList As New List(Of PCCCSubscription)
    Private GroupedSubscriptionReads As New System.Collections.Concurrent.ConcurrentDictionary(Of Integer, SubscriptionRead)
    Private SubscriptionThread As System.ComponentModel.BackgroundWorker
    Private SubscriptionListChanges As Integer

#Region "Constructor"
    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        MyClass.New()

        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)
    End Sub

    Public Sub New()
        MyBase.New()
    End Sub

    'Friend MustOverride Sub CreateDLLInstance()


    'Component overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        '* Stop the subscription thread
        StopSubscriptions = True

        System.Threading.Thread.Sleep(250)

        '     RemoveDLLConnection()

        MyBase.Dispose(disposing)
    End Sub
#End Region


#Region "Properties"
    Private m_ProcessorType As Integer = &H13
    Private ReadOnly Property ProcessorType As Integer
        Get
            Return m_ProcessorType
            'Return GetProcessorType()
        End Get
    End Property

    '*************************************************************************************************
    '* If set to other than 0, this will be the poll rate irrelevant of the value passed to subscribe
    '*************************************************************************************************
    Private m_PollRateOverride As Integer = 500
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property PollRateOverride() As Integer
        Get
            Return m_PollRateOverride
        End Get
        Set(ByVal value As Integer)
            If value >= 0 Then
                m_PollRateOverride = value
            End If
        End Set
    End Property


    '**************************************************************
    '* Stop the polling of subscribed data
    '**************************************************************
    Private m_DisableSubscriptions As Boolean
    Public Property DisableSubscriptions() As Boolean Implements MfgControl.AdvancedHMI.Drivers.IComComponent.DisableSubscriptions
        Get
            Return m_DisableSubscriptions
        End Get
        Set(ByVal value As Boolean)
            m_DisableSubscriptions = value
        End Set
    End Property

    '**************************************************
    '* Its purpose is to fetch
    '* the main form in order to synchronize the
    '* notification thread/event
    '**************************************************
    'Private m_SynchronizingObject As System.ComponentModel.ISynchronizeInvoke
    '* do not let this property show up in the property window
    ' <System.ComponentModel.Browsable(False)> _
    Public MustOverride Property SynchronizingObject() As System.ComponentModel.ISynchronizeInvoke
#End Region

#Region "Subscriptions"
    '* This is used to optimize the reads of the subscriptions
    Private Class SubscriptionRead
        Friend Address As String
        Friend NumberToRead As Integer
    End Class

    Public Function Subscribe(ByVal PLCAddress As String, ByVal numberOfElements As Int16, ByVal PollRate As Integer, ByVal CallBack As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)) As Integer Implements MfgControl.AdvancedHMI.Drivers.IComComponent.Subscribe
        Dim ParsedResult As New PCCCSubscription(PLCAddress, 1, ProcessorType)

        ParsedResult.PollRate = PollRate
        ParsedResult.dlgCallBack = CallBack
        'ParsedResult.TargetNode = m_TargetNode

        SubscriptionList.Add(ParsedResult)

        SubscriptionList.Sort(AddressOf SortPolledAddresses)


        '* Start the subscription thread
        If SubscriptionThread Is Nothing Then
            SubscriptionThread = New System.ComponentModel.BackgroundWorker
            AddHandler SubscriptionThread.DoWork, AddressOf SubscriptionUpdate
            SubscriptionThread.RunWorkerAsync()
        End If

        '* Flag this so it will run the optimizer after the first read
        SubscriptionListChanges += 1

        Return ParsedResult.ID
    End Function

    '***************************************************************
    '* Used to sort polled addresses by File Number and element
    '* This helps in optimizing reading
    '**************************************************************
    Private Function SortPolledAddresses(ByVal A1 As PCCCSubscription, ByVal A2 As PCCCSubscription) As Integer
        If A1.FileNumber = A2.FileNumber Then
            If A1.Element > A2.Element Then
                Return 1
            ElseIf A1.Element = A2.Element Then
                Return 0
            Else
                Return -1
            End If
        End If

        If A1.FileNumber > A2.FileNumber Then
            Return 1
        Else
            Return -1
        End If
    End Function

    Private UnsubscribeLock As New Object
    Public Function UnSubscribe(ByVal ID As Integer) As Integer Implements MfgControl.AdvancedHMI.Drivers.IComComponent.Unsubscribe
        Dim i As Integer = 0
        While i < SubscriptionList.Count AndAlso SubscriptionList(i).ID <> ID
            i += 1
        End While

        SyncLock (UnsubscribeLock)
            If i < SubscriptionList.Count Then
                SubscriptionList.RemoveAt(i)
                '* update group list unless the subscriptions are being stopped
                If Not StopSubscriptions Then
                    CreateGroupedReadList()
                End If
            End If
        End SyncLock
    End Function

    '************************************************************************
    '* This is used to optimize subscription updates by grouping addresses
    '* that are close together and in the same data table into a single read
    '*  It optimizes subscription updates for speed
    '************************************************************************
    Private Sub CreateGroupedReadList()
        GroupedSubscriptionReads.Clear()

        Dim i, NumberToRead, FirstElement As Integer
        Dim HighestBit As Integer = SubscriptionList(i).BitNumber
        While i < SubscriptionList.Count
            Dim NumberToReadCalc As Integer
            NumberToRead = SubscriptionList(i).NumberOfElements
            FirstElement = i
            Dim PLCAddress As String = SubscriptionList(FirstElement).PLCAddress

            '* Group into the same read if there is less than a 20 element gap
            '* Do not group IO addresses because they can exceed 16 bits which causes problems
            Dim ElementSpan As Integer = 20
            '* ARJ 2-NOV-11 Changed but not tested to fix a limit of 59 float subscriptions
            While i < SubscriptionList.Count - 1 AndAlso (SubscriptionList(i).FileNumber = SubscriptionList(i + 1).FileNumber And _
                            ((SubscriptionList(i + 1).Element - SubscriptionList(FirstElement).Element < 20 And SubscriptionList(i).FileType <> &H8B And SubscriptionList(i).FileType <> &H8C) Or _
                                SubscriptionList(i + 1).Element = SubscriptionList(i).Element))
                NumberToReadCalc = SubscriptionList(i + 1).Element - SubscriptionList(FirstElement).Element + SubscriptionList(i + 1).NumberOfElements
                If NumberToReadCalc > NumberToRead Then NumberToRead = NumberToReadCalc

                '* This is used for IO addresses wher the bit can be above 15
                If SubscriptionList(i).BitNumber < 99 And SubscriptionList(i).BitNumber > HighestBit Then HighestBit = SubscriptionList(i).BitNumber

                i += 1
            End While

            '*****************************************************
            '* IO addresses can exceed bit 15 on the same element
            '*****************************************************
            If SubscriptionList(FirstElement).FileType = &H8B Or SubscriptionList(FirstElement).FileType = &H8C Then
                If SubscriptionList(FirstElement).FileType = SubscriptionList(i).FileType Then
                    If HighestBit > 15 And HighestBit < 99 Then
                        If ((HighestBit >> 4) + 1) > NumberToRead Then
                            NumberToRead = (HighestBit >> 4) + 1
                        End If
                    End If
                End If
            End If


            '* Get file type designation.
            '* Is it more than one character (e.g. "ST")
            If SubscriptionList(FirstElement).PLCAddress.Substring(1, 1) >= "A" And SubscriptionList(FirstElement).PLCAddress.Substring(1, 1) <= "Z" Then
                PLCAddress = SubscriptionList(FirstElement).PLCAddress.Substring(0, 2) & SubscriptionList(FirstElement).FileNumber & ":" & SubscriptionList(FirstElement).Element
            Else
                PLCAddress = SubscriptionList(FirstElement).PLCAddress.Substring(0, 1) & SubscriptionList(FirstElement).FileNumber & ":" & SubscriptionList(FirstElement).Element
            End If


            Dim s As New SubscriptionRead
            s.Address = PLCAddress
            s.NumberToRead = NumberToRead

            GroupedSubscriptionReads.TryAdd(GroupedSubscriptionReads.Count, s)

            i += 1
        End While


        If SubscriptionListChanges > 0 Then
            SubscriptionListChanges -= 1
        End If
    End Sub

    '************************************************************************************
    '* This runs under a background thread to continuously update the subscribed values
    '************************************************************************************
    Private StopSubscriptions As Boolean
    Private Sub SubscriptionUpdate(sender As System.Object, e As System.ComponentModel.DoWorkEventArgs)
        Dim ReadTime As New Stopwatch
        While Not StopSubscriptions
            If Not m_DisableSubscriptions And GroupedSubscriptionReads IsNot Nothing Then
                '* 3-JUN-13 Do not read data until handles are created to avoid exceptions
                If SynchronizingObject Is Nothing OrElse DirectCast(SynchronizingObject, Windows.Forms.Control).IsHandleCreated Then
                    Dim DelayBetweenPackets As Integer
                    Dim response As Integer
                    Dim TransactionNumber As Integer
                    'index = 0
                    For Each key In GroupedSubscriptionReads.Keys
                        'While index < GroupedSubscriptionReads.Count And Not StopSubscriptions
                        '* Evenly space out read requests to avoid Send Que Full
                        DelayBetweenPackets = Convert.ToInt32(Math.Max(1, Math.Floor(m_PollRateOverride / GroupedSubscriptionReads.Count)))
                        ReadTime.Start()

                        Try
                            If Not m_DisableSubscriptions And Not StopSubscriptions Then
                                TransactionNumber = Me.BeginRead(GroupedSubscriptionReads(key).Address, GroupedSubscriptionReads(key).NumberToRead)
                                response = WaitForResponse(TransactionNumber)

                                Try
                                    If response = 0 And Not StopSubscriptions Then
                                        SendToSubscriptions(Responses(TransactionNumber And 255))
                                    Else
                                        Dim dbg = 0
                                    End If
                                Catch ex As Exception
                                    Throw
                                End Try

                            End If
                        Catch ex As MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException
                            Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(ex.ErrorCode, ex.Message)
                            Try
                                SendToSubscriptions(x)
                            Catch ex1 As Exception
                                Dim dbg = 0
                            End Try
                        Catch ex As ObjectDisposedException
                            '* Object disposed means they Transport or DLL was closed and disposed
                            StopSubscriptions = True
                            Exit For
                        Catch ex As Exception
                            '* Send this message back to the requesting control
                            Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(-99, ex.Message)
                            SendToSubscriptions(x)
                        End Try

                        ReadTime.Stop()

                        '* Evenly space out the reads to avoid SendQue Full
                        If Convert.ToInt32(ReadTime.ElapsedMilliseconds) < DelayBetweenPackets Then
                            Threading.Thread.Sleep(DelayBetweenPackets - Convert.ToInt32(ReadTime.ElapsedMilliseconds))
                        End If

                        ReadTime.Reset()
                    Next
                End If
            End If

            If SubscriptionListChanges > 0 And Not StopSubscriptions Then
                CreateGroupedReadList()
            End If
        End While
    End Sub

    '**************************************************************************
    '* After data is returned from the PLC, check to see if any subscriptions
    '* use this data. If so, extract the required data and send to subscriber
    '**************************************************************************
    Private Sub SendToSubscriptions(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        Dim TNSLowerByte As Integer = e.TransactionNumber And 255

        If (Requests(TNSLowerByte) Is Nothing) OrElse (e.Values.Count <= 0 And e.ErrorId = 0) Then
            Exit Sub
        End If

        Dim i As Integer
        Dim EnoughElements As Boolean
        Dim d(e.Values.Count - 1) As String
        For n = 0 To d.Length - 1
            d(n) = e.Values(n)
        Next

        '* 07-MAR-12 V1.12 If a subscription was deleted, then ignore
        SyncLock (UnsubscribeLock)
            Dim SavedCount As Integer = SubscriptionList.Count
            While i < SubscriptionList.Count
                ''*********************************************************
                ''* Check to see if this is from the Polled variable list
                ''*********************************************************
                EnoughElements = False                    '* Are there enought elements read for this request
                '* Version 3.98t - changed <= to <
                If (SubscriptionList(i).Element - Requests(TNSLowerByte).Element + SubscriptionList(i).NumberOfElements <= d.Length) And _
                    (SubscriptionList(i).FileType <> 134 And SubscriptionList(i).FileType <> 135 And SubscriptionList(i).FileType <> &H8B And SubscriptionList(i).FileType <> &H8C) Then
                    EnoughElements = True
                End If
                '* Version 3.98t - changed <= to <
                If (SubscriptionList(i).BitNumber < 16) And (((SubscriptionList(i).Element - Requests(TNSLowerByte).Element) + Math.Ceiling(SubscriptionList(i).NumberOfElements / 16)) <= d.Length) Then
                    EnoughElements = True
                End If
                If (SubscriptionList(i).FileType = 134 Or SubscriptionList(i).FileType = 135) And (SubscriptionList(i).Element - Requests(TNSLowerByte).Element + SubscriptionList(i).NumberOfElements) <= d.Length Then
                    EnoughElements = True
                End If
                '* IO addresses - be sure not to cross elements/card slots
                If (SubscriptionList(i).FileType = &H8B Or SubscriptionList(i).FileType = &H8C And _
                        SubscriptionList(i).Element = Requests(TNSLowerByte).Element) Then
                    '* 03-MAY-12 Added check for bitnumber being 99, ReAdded 28-SEP-12
                    Dim WordToUse As Integer
                    If SubscriptionList(i).BitNumber = 99 Then
                        WordToUse = 0
                    Else
                        WordToUse = SubscriptionList(i).BitNumber >> 4
                    End If
                    If (d.Length - 1) >= (SubscriptionList(i).Element - Requests(TNSLowerByte).Element + (WordToUse)) Then
                        EnoughElements = True
                    End If
                End If


                If SubscriptionList(i).FileNumber = Requests(TNSLowerByte).FileNumber And _
                    EnoughElements And _
                    Requests(TNSLowerByte).Element <= SubscriptionList(i).Element Then ' And _
                    '((PLCAddressByTNS(TNSReturned).FileType <> &H8B And PLCAddressByTNS(TNSReturned).FileType <> &H8C) Or PLCAddressByTNS(TNSReturned).BitNumber = SubscriptionList(i).BitNumber) Then
                    'SubscriptionList(i).BitNumber = PLCAddressByTNS(TNSReturned).BitNumber Then
                    'PolledValueReturned(PLCAddressByTNS(TNSReturned).PLCAddress, d)

                    Dim BitResult(SubscriptionList(i).NumberOfElements - 1) As String
                    '* Handle timers,counters, and R (control) as exceptions because of the 3 subelements
                    If (SubscriptionList(i).FileType = 134 Or SubscriptionList(i).FileType = 135 Or SubscriptionList(i).FileType = 136) Then
                        '* If this is a bit level address for a timer or counter, then handle appropriately
                        If SubscriptionList(i).BitNumber < 16 Then
                            Try
                                '                                    If d((SubscriptionList(i).Element- PLCAddressByTNS(TNSReturned).Element) * 3) Then
                                BitResult(0) = Convert.ToString((Convert.ToInt32(d((SubscriptionList(i).Element - Requests(TNSLowerByte).Element) * 3)) And Convert.ToInt32(2 ^ SubscriptionList(i).BitNumber)) > 0)
                                'End If
                            Catch ex As Exception
                                System.Windows.Forms.MessageBox.Show("Error in returning data from datareceived")
                            End Try
                        Else
                            Try
                                For k As Integer = 0 To SubscriptionList(i).NumberOfElements - 1
                                    BitResult(k) = d((SubscriptionList(i).Element - Requests(TNSLowerByte).Element + k) * 3 + SubscriptionList(i).SubElement)
                                Next
                            Catch ex As Exception
                                System.Windows.Forms.MessageBox.Show("Error in returning data from datareceived")
                            End Try
                        End If

                        Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(BitResult, SubscriptionList(i).PLCAddress, e.TransactionNumber)
                        x.SubscriptionID = SubscriptionList(i).ID
                        Dim z() As Object = {Me, x}
                        Try
                            SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)
                        Catch ex As InvalidOperationException
                            '* Ignore this error. It's caused when the handle is not created
                        End Try

                        'SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, New Object() {BitResult})
                    Else
                        '* If its bit level, then return the individual bit
                        If SubscriptionList(i).BitNumber < 99 Then
                            '*TODO : Make this handle a rquest for multiple bits
                            Try
                                '* Test to see if bits or integers returned
                                'Dim x As Integer
                                Try
                                    'x = d(0)
                                    If SubscriptionList(i).BitNumber < 16 Then
                                        Dim ValueToExtractBitFrom As Int32 = Convert.ToInt32(d(SubscriptionList(i).Element - Requests(TNSLowerByte).Element))
                                        BitResult(0) = Convert.ToString((ValueToExtractBitFrom And Convert.ToInt32(2 ^ SubscriptionList(i).BitNumber)) > 0)
                                    Else
                                        Dim WordToUse As Integer = SubscriptionList(i).BitNumber >> 4
                                        Dim ModifiedBitToUse As Integer = SubscriptionList(i).BitNumber Mod 16
                                        BitResult(0) = Convert.ToString((Convert.ToInt32(d(SubscriptionList(i).Element - Requests(TNSLowerByte).Element + (WordToUse))) And Convert.ToInt32(2 ^ ModifiedBitToUse)) > 0)
                                    End If
                                Catch ex As Exception
                                    BitResult(0) = d(0)
                                End Try
                            Catch ex As Exception
                                System.Windows.Forms.MessageBox.Show("Error in returning data from datareceived - " & ex.Message)
                            End Try
                            Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(BitResult, SubscriptionList(i).PLCAddress, e.TransactionNumber)
                            x.SubscriptionID = SubscriptionList(i).ID
                            Dim z() As Object = {Me, x}
                            SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)

                            'SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, New Object() {BitResult})
                        Else
                            '* All other data types
                            For k As Integer = 0 To SubscriptionList(i).NumberOfElements - 1
                                BitResult(k) = d((SubscriptionList(i).Element - Requests(TNSLowerByte).Element + k))
                            Next

                            Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(BitResult, SubscriptionList(i).PLCAddress, e.TransactionNumber)
                            x.SubscriptionID = SubscriptionList(i).ID
                            Dim z() As Object = {Me, x}
                            SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)

                            'm_SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, d(SubscriptionList(i).PLCAddress.Element- PLCAddressByTNS(TNSReturned).Element))
                            'SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, New Object() {BitResult})

                        End If
                    End If
                End If

                i += 1
            End While
        End SyncLock
    End Sub
#End Region


#Region "Data Reading"
    '* Retreives the processor code by using the get status command
    Public Function GetProcessorType() As Integer
        '* Uncomment the following to bypass this check
        'Return &H78
        If m_ProcessorType <> 0 Then
            Return m_ProcessorType
        Else
            Dim TNS As Integer = GetNextTNSNumber()
            Dim TNSLowByte As Integer = TNS And 255
            Requests(TNSLowByte) = New MfgControl.AdvancedHMI.Drivers.PCCCAddress
            DiagnosticStatus(TNS)

            If WaitForResponse(TNS) = 0 Then
                '* Returned data position 11 is the first character in the ASCII name of the processor
                '* Position 9 is the code for the processor
                '* &H15 = PLC 5/40
                '* &H18 = SLC 5/01
                '* &H1A = Fixed SLC500
                '* &H21 = PLC 5/60
                '* &H22 = PLC 5/10
                '* &H23 = PLC 5/60
                '* &H25 = SLC 5/02
                '* &H28 = PLC 5/40
                '* &H29 = PLC 5/60
                '* &H31 = PLC 5/11
                '* &H33 = PLC 5/30
                '* &H49 = SLC 5/03
                '* &H58 = ML1000
                '* &H5B = SLC 5/04
                '* &H6F = SLC 5/04 (L541)
                '* &H78 = SLC 5/05
                '* &H83 = PLC 5/30E
                '* &H84 = ENI Module
                '* &H86 = PLC 5/80E
                '* &H88 = ML1200
                '* &H89 = ML1500 LSP
                '* &H8C = ML1500 LRP
                '* &H95 = CompactLogix L35E
                '* &H9C = ML1100
                '* &H4A = L20E
                '* &H4B = L40E
                '* SLC 500
                If RawResponses(TNSLowByte).EncapsulatedData(1) = &HEE Then
                    m_ProcessorType = RawResponses(TNSLowByte).EncapsulatedData(3)
                ElseIf RawResponses(TNSLowByte).EncapsulatedData(1) = &HFE Or RawResponses(TNSLowByte).EncapsulatedData(1) = &HEB Then
                    m_ProcessorType = RawResponses(TNSLowByte).EncapsulatedData(2)
                Else
                    m_ProcessorType = RawResponses(TNSLowByte).EncapsulatedData(6)
                End If
                Return ProcessorType
            Else
                Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to get processor type")
            End If
        End If
    End Function

    Private ReadLock2 As New Object
    Public Function Read(ByVal startAddress As String, ByVal numberOfElements As Integer) As String() Implements MfgControl.AdvancedHMI.Drivers.IComComponent.Read
        Dim TNS As Integer
        SyncLock (ReadLock2)
            TNS = BeginRead(startAddress, numberOfElements)

            Dim result As Integer
            'result = WaitForResponse(TNS)

            If Responses(TNS And 255) Is Nothing Then
                Dim x = Requests(TNS And 255)
                Dim dbg = 0
            End If

            If result = 0 Then
                Dim d(Responses(TNS And 255).Values.Count - 1) As String
                Responses(TNS And 255).Values.CopyTo(d, 0)
                Return d
            Else
                Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(result, "Read Failed - Result=" & result & ". " & DecodeMessage(result))
            End If
        End SyncLock
    End Function

    Public Function Read(ByVal startAddress As String) As String
        Return Read(startAddress, 1)(0)
    End Function


    '*************************************************************
    '* Overloaded method of ReadAny - that reads only one element
    '*************************************************************
    Public Function BeginRead(ByVal startAddress As String) As Integer
        Return BeginRead(startAddress, 1)
    End Function

    'Public Function BeginRead(ByVal startAddress As String, ByVal numberOfElements As Integer) As String()
    '    Return ReadAny(startAddress, numberOfElements, m_AsyncMode)
    'End Function

    Private ReadLock As New Object
    '******************************************
    '* Synchronous read of any data type
    '*  this function does not declare its return type because it dependent on the data type read
    '******************************************
    Public Function BeginRead(ByVal startAddress As String, ByVal numberOfElements As Integer) As Integer Implements MfgControl.AdvancedHMI.Drivers.IComComponent.BeginRead
        Dim data(4) As Byte
        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, numberOfElements, m_ProcessorType)  ' GetProcessorType)
        'ParsedResult.TargetNode = m_TargetNode

        SyncLock (ReadLock)
            '* If requesting 0 elements, then default to 1
            Dim ArrayElements As Integer = numberOfElements - 1
            If ArrayElements < 0 Then
                ArrayElements = 0
            End If

            '* If reading at bit level ,then convert number bits to read to number of words
            '* Fixed a problem when reading multiple bits that span over more than 1 word
            If ParsedResult.BitNumber < 99 Then
                ArrayElements = Convert.ToInt32(Math.Floor(((numberOfElements + ParsedResult.BitNumber) - 1) / 16))
            End If


            '* Number of bytes to read
            Dim NumberOfBytes As Integer


            NumberOfBytes = (ArrayElements + 1) * ParsedResult.BytesPerElement


            '* If it is a multiple read of sub-elements of timers and counter, then read an array of the same consectutive sub element
            '* FIX
            If ParsedResult.SubElement > 0 AndAlso ArrayElements > 0 AndAlso (ParsedResult.FileType = &H86 Or ParsedResult.FileType = &H87) Then
                NumberOfBytes = (NumberOfBytes * 3)   '* There are 3 words per sub element (6 bytes)
            End If


            '* 23-MAY-13 - Added AND 255 to keep from overflowing
            Try
                ParsedResult.ByteStream(0) = CByte(NumberOfBytes And 255)
            Catch ex As Exception
                Throw ex
            End Try

            Return ReadRawData(ParsedResult)
        End SyncLock
    End Function

    Private Shared Function ExtractData(ByVal ParsedResult As MfgControl.AdvancedHMI.Drivers.PCCCAddress, ByVal ReturnedData() As Byte) As String()
        '* Get the element size in bytes
        Dim ElementSize As Integer = ParsedResult.BytesPerElement

        '***************************************************
        '* Extract returned data into appropriate data type
        '***************************************************
        'Dim result(Math.Floor((ParsedResult.NumberOfElements * ParsedResult.BytesPerElement) / ElementSize) - 1) As String
        '* 18-MAY-12 Changed to accomodate packet being broken up and reassembled by ReadRawData
        Dim result(Convert.ToInt32(Math.Floor(ReturnedData.Length / ElementSize) - 1)) As String

        Dim StringLength As Integer
        Select Case ParsedResult.FileType
            Case &H8A '* Floating point read (&H8A)
                For i As Integer = 0 To result.Length - 1
                    result(i) = Convert.ToString(BitConverter.ToSingle(ReturnedData, (i * ParsedResult.BytesPerElement)))
                Next
            Case &H8D ' * String
                For i As Integer = 0 To result.Length - 1
                    StringLength = BitConverter.ToInt16(ReturnedData, (i * ParsedResult.BytesPerElement))
                    '* The controller may falsely report the string length, so set to max allowed
                    If StringLength > 82 Then StringLength = 82

                    '* use a string builder for increased performance
                    Dim result2 As New System.Text.StringBuilder
                    Dim j As Integer = 2
                    '* Stop concatenation if a zero (NULL) is reached
                    While j < StringLength + 2 And ReturnedData((i * 84) + j + 1) > 0
                        result2.Append(Convert.ToChar(ReturnedData((i * 84) + j + 1)))
                        '* Prevent an odd length string from getting a Null added on
                        If j < StringLength + 1 And (ReturnedData((i * 84) + j)) > 0 Then result2.Append(Convert.ToChar(ReturnedData((i * 84) + j)))
                        j += 2
                    End While
                    result(i) = result2.ToString
                Next
            Case &H86, &H87, &H88  '* Timer, counter, control
                '* If a sub element is designated then read the same sub element for all timers
                Dim j As Integer
                If ParsedResult.SubElement > 0 Then
                    For i = 0 To ParsedResult.NumberOfElements - 1
                        j = i * 6 + (ParsedResult.SubElement * 2)
                        result(i) = Convert.ToString(BitConverter.ToInt16(ReturnedData, j))
                    Next
                Else
                    For i2 = 0 To (ParsedResult.NumberOfElements * 3) - 1
                        j = i2 * 2
                        result(i2) = Convert.ToString(BitConverter.ToInt16(ReturnedData, j))
                    Next
                End If

            Case &H91 '* Long Value read (&H91)
                For i As Integer = 0 To result.Length - 1
                    result(i) = Convert.ToString(BitConverter.ToInt32(ReturnedData, (i * ParsedResult.BytesPerElement)))
                Next
            Case &H92 '* MSG Value read (&H92)
                For i As Integer = 0 To result.Length - 1
                    result(i) = BitConverter.ToString(ReturnedData, (i * ParsedResult.BytesPerElement), 50)
                Next
            Case Else
                For i As Integer = 0 To result.Length - 1
                    result(i) = Convert.ToString(BitConverter.ToInt16(ReturnedData, (i * ParsedResult.BytesPerElement)))
                Next
        End Select
        'End If


        '******************************************************************************
        '* If the number of words to read is not specified, then return a single value
        '******************************************************************************
        '* Is it a bit level and N or B file?
        If ParsedResult.BitNumber >= 0 And ParsedResult.BitNumber < 99 Then
            Dim BitResult(ParsedResult.NumberOfElements - 1) As String
            Dim BitPos As Integer = ParsedResult.BitNumber
            Dim WordPos As Integer = 0
            'Dim Result(ArrayElements) As Boolean

            '* If a bit number is greater than 16, point to correct word
            '* This can happen on IO addresses (e.g. I:0/16)
            WordPos += BitPos >> 4
            BitPos = BitPos Mod 16

            '* Set array of consectutive bits
            For i As Integer = 0 To BitResult.Length - 1
                BitResult(i) = Convert.ToString((Convert.ToInt32(result(WordPos)) And Convert.ToInt32(2 ^ BitPos)) > 0)
                BitPos += 1
                If BitPos > 15 Then
                    BitPos = 0
                    WordPos += 1
                End If
            Next
            Return BitResult
        End If

        Return result
    End Function

    '***********************************************
    ' Reads values and returns them as integers
    '***********************************************
    Public Function ReadInt(ByVal startAddress As String, ByVal numberOfBytes As Integer) As Integer()
        Dim result() As String
        result = Read(startAddress, numberOfBytes)

        Dim Ints(result.Length) As Integer
        For i As Integer = 0 To result.Length - 1
            Ints(i) = Convert.ToInt32(result(i))
        Next

        Return Ints
    End Function

    '*********************************************************************************
    '* Read Raw File data and break up into chunks because of limits of DF1 protocol
    '*********************************************************************************
    Private Function ReadRawData(ByVal PAddressO As MfgControl.AdvancedHMI.Drivers.PCCCAddress) As Integer
        '* Create a clone to work with so we do not modifiy the original through a pointer
        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(m_ProcessorType)
        PAddress = DirectCast(PAddressO.Clone, MfgControl.AdvancedHMI.Drivers.PCCCAddress)

        Dim NumberOfBytesToRead, FilePosition As Integer
        Dim AccumulatedValues(PAddress.ByteSize - 1) As Byte
        Dim Result As Integer
        Dim TNS As Integer

        Do While FilePosition < PAddress.ByteSize AndAlso Result = 0
            '* Set next length of data to read. Max of 236 (slc 5/03 and up)
            '* This must limit to 82 for 5/02 and below
            If PAddress.ByteSize - FilePosition < 236 Then
                NumberOfBytesToRead = PAddress.ByteSize - FilePosition
            Else
                NumberOfBytesToRead = 236
            End If


            '* String is an exception
            If NumberOfBytesToRead > 168 AndAlso PAddress.FileType = &H8D Then
                '* Only two string elements can be read on each read (168 bytes)
                NumberOfBytesToRead = 168
            End If

            If NumberOfBytesToRead > 234 AndAlso (PAddress.FileType = &H86 OrElse PAddress.FileType = &H87) Then
                '* Timers & counters read in multiples of 6 bytes
                NumberOfBytesToRead = 234
            End If

            If NumberOfBytesToRead > 0 Then
                Dim DataSize As Integer

                DataSize = 4

                '**********************************************************************
                '* Link the TNS to the original address for use by the linked polling
                '**********************************************************************
                TNS = GetNextTNSNumber()
                Dim TNSLowerByte As Integer = TNS And 255

                'PAddressO.TargetNode = m_TargetNode
                Requests(TNSLowerByte) = PAddressO
                Requests(TNSLowerByte).Responded = False

                '* A PLC specifies the number of bytes at the end of the stream
                'PAddress.ByteStream(PAddress.ByteStream.Length - 1) = CByte(NumberOfBytesToRead)
                'WordRangeRead(CByte(NumberOfBytesToRead And 255), PAddress, TNS)
                WordRangeRead(PAddress, TNS)


                Result = WaitForResponse(TNS)

                If Result = 0 Then
                    If (FilePosition + NumberOfBytesToRead < PAddress.ByteSize) Then
                        '* Return status byte that came from controller
                        If RawResponses(TNSLowerByte).EncapsulatedData IsNot Nothing Then
                            Result = CInt(RawResponses(TNSLowerByte).Status)  '* STS position in DF1 message
                        Else
                            Result = -8 '* no response came back from PLC
                        End If
                    End If

                    '***************************************************
                    '* Extract returned data into appropriate data type
                    '* Transfer block of data read to the data table array
                    '***************************************************
                    '* TODO: Check array bounds
                    'If Result = 0 Then
                    'Dim x = RawResponses(TNSLowerByte).EncapsulatedData
                    If Result = 0 Then
                        For i As Integer = 0 To NumberOfBytesToRead - 1
                            AccumulatedValues(FilePosition + i) = RawResponses(TNSLowerByte).EncapsulatedData(i) ' + 6)
                            ' Responses(TNSLowerByte).Values.Add(AccumulatedValues(FilePosition + i))
                        Next
                        FilePosition += NumberOfBytesToRead

                        '* point to the next element
                        If PAddress.FileType = &HA4 Then
                            PAddress.Element += Convert.ToInt32(NumberOfBytesToRead / &H28)
                        ElseIf PAddress.FileType = &H8A Then
                            PAddress.Element += Convert.ToInt32(NumberOfBytesToRead / 4)
                        ElseIf PAddress.FileType = &H89 Then
                            PAddress.Element += Convert.ToInt32(NumberOfBytesToRead / 2)
                        Else
                            '* Use subelement because it works with all data file types
                            PAddress.SubElement += Convert.ToInt32(NumberOfBytesToRead / 2)
                        End If
                    End If
                Else
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(DecodeMessage(Result))
                End If
            End If
        Loop

        If Result <> 0 Then
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(DecodeMessage(Result))
        End If

        '* Grab a TNS number
        TNS = GetNextTNSNumber()
        Dim TNSLowerByte2 As Integer = TNS And 255
        Responses(TNSLowerByte2) = New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(AccumulatedValues, PAddressO.PLCAddress, CUShort(TNS))
        Requests(TNSLowerByte2) = PAddressO

        '***************************************************
        '* Extract returned data into appropriate data type
        '* Transfer block of data read to the data table array
        '***************************************************
        Dim d() As String
        d = ExtractData(Requests(TNSLowerByte2), AccumulatedValues)

        For i = 0 To d.Length - 1
            Responses(TNSLowerByte2).Values.Add(d(i))
        Next

        Return TNS
    End Function
#End Region

#Region "Data Writing"
    '*****************************************************************
    '* Write Section
    '*
    '* Address is in the form of <file type><file Number>:<offset>
    '* examples  N7:0, B3:0,
    '******************************************************************

    '* Handle one value of Integer type
    '* Write a single integer value to a PLC data table
    '* The startAddress is in the common form of AB addressing (e.g. N7:0)
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite As Integer) As Integer
        Dim temp(1) As Integer
        temp(0) = dataToWrite
        Return Write(startAddress, 1, temp)
    End Function


    '* Write an array of integers
    '* Write multiple consectutive integer values to a PLC data table
    '* The startAddress is in the common form of AB addressing (e.g. N7:0)
    Public Function Write(ByVal startAddress As String, ByVal numberOfElements As Integer, ByVal dataToWrite() As Integer) As Integer
        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, numberOfElements, m_ProcessorType)

        Dim ConvertedData(numberOfElements * ParsedResult.BytesPerElement - 1) As Byte

        Dim i As Integer
        If ParsedResult.FileType = &H91 Then
            '* Write to a Long integer file
            While i < numberOfElements
                '******* NOT Necesary to validate because dataToWrite keeps it in range for a long
                Dim b(3) As Byte
                b = BitConverter.GetBytes(dataToWrite(i))

                ConvertedData(i * 4) = b(0)
                ConvertedData(i * 4 + 1) = b(1)
                ConvertedData(i * 4 + 2) = b(2)
                ConvertedData(i * 4 + 3) = b(3)
                i += 1
            End While
        ElseIf ParsedResult.FileType <> 0 Then
            While i < numberOfElements
                '* Validate range
                If dataToWrite(i) > 32767 Or dataToWrite(i) < -32768 Then
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Integer data out of range, must be between -32768 and 32767")
                End If

                ConvertedData(i * 2) = CByte(dataToWrite(i) And &HFF)
                ConvertedData(i * 2 + 1) = CByte((dataToWrite(i) >> 8) And &HFF)

                i += 1
            End While
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Invalid Address")
        End If

        Return WriteRawData(ParsedResult, numberOfElements * ParsedResult.BytesPerElement, ConvertedData)
    End Function

    '* Handle one value of Single type
    '* Write a single floating point value to a data table
    '* The startAddress is in the common form of AB addressing (e.g. F8:0)
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite As Single) As Integer
        Dim temp(1) As Single
        temp(0) = dataToWrite
        Return Write(startAddress, 1, temp)
    End Function

    '* Write an array of Singles
    '* Write multiple consectutive floating point values to a PLC data table
    '* The startAddress is in the common form of AB addressing (e.g. F8:0)
    Public Function Write(ByVal startAddress As String, ByVal numberOfElements As Integer, ByVal dataToWrite() As Single) As Integer
        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, numberOfElements, GetProcessorType)

        Dim ConvertedData(numberOfElements * ParsedResult.BytesPerElement) As Byte

        Dim i As Integer
        If ParsedResult.FileType = &H8A Then
            '*Write to a floating point file
            Dim bytes(4) As Byte
            For i = 0 To numberOfElements - 1
                bytes = BitConverter.GetBytes(CSng(dataToWrite(i)))
                For j As Integer = 0 To 3
                    ConvertedData(i * 4 + j) = CByte(bytes(j))
                Next
            Next
        ElseIf ParsedResult.FileType = &H91 Then
            '* Write to a Long integer file
            While i < numberOfElements
                '* Validate range
                If dataToWrite(i) > 2147483647 Or dataToWrite(i) < -2147483648 Then
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Integer data out of range, must be between -2147483648 and 2147483647")
                End If

                Dim b(3) As Byte
                b = BitConverter.GetBytes(Convert.ToInt32(dataToWrite(i)))

                ConvertedData(i * 4) = b(0)
                ConvertedData(i * 4 + 1) = b(1)
                ConvertedData(i * 4 + 2) = b(2)
                ConvertedData(i * 4 + 3) = b(3)
                i += 1
            End While
        Else
            '* Write to an integer file
            While i < numberOfElements
                '* Validate range
                If dataToWrite(i) > 32767 Or dataToWrite(i) < -32768 Then
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Integer data out of range, must be between -32768 and 32767")
                End If

                ConvertedData(i * 2) = CByte(Convert.ToInt32(dataToWrite(i)) And &HFF)
                ConvertedData(i * 2 + 1) = CByte((Convert.ToInt32(dataToWrite(i)) >> 8) And &HFF)
                i += 1
            End While
        End If

        Return WriteRawData(ParsedResult, numberOfElements * ParsedResult.BytesPerElement, ConvertedData)
    End Function

    '* Write a String
    '* Write a string value to a string data table
    '* The startAddress is in the common form of AB addressing (e.g. ST9:0)
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite As String) As String Implements MfgControl.AdvancedHMI.Drivers.IComComponent.Write
        If dataToWrite Is Nothing Then
            Return "0"
        End If

        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, 1, GetProcessorType)

        If ParsedResult.FileType = 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Invalid Address")

        If startAddress.IndexOf("ST", StringComparison.OrdinalIgnoreCase) >= 0 Then
            '* Add an extra character to compensate for characters written in pairs to integers
            'Dim ConvertedData(dataToWrite.Length + 2 + 1) As Byte
            Dim ConvertedData(83) As Byte
            dataToWrite &= Convert.ToChar(0)

            ConvertedData(0) = CByte(dataToWrite.Length - 1)
            Dim i As Integer = 2
            Dim StringBytes() As Byte = System.Text.Encoding.ASCII.GetBytes(dataToWrite)
            While i <= dataToWrite.Length
                ConvertedData(i + 1) = StringBytes(i - 2)
                ConvertedData(i) = StringBytes(i - 1)
                i += 2
            End While

            Return Convert.ToString(WriteRawData(ParsedResult, 84, ConvertedData))
        ElseIf startAddress.IndexOf("L", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return Convert.ToString(Write(startAddress, Convert.ToInt32(dataToWrite)))
        ElseIf startAddress.IndexOf("F", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return Convert.ToString(Write(startAddress, CSng(dataToWrite)))
        Else
            Return Convert.ToString(Write(startAddress, Convert.ToInt32(dataToWrite)))
        End If
    End Function


    Public Function BeginWrite(ByVal startAddress As String, ByVal numberOfElemens As Integer, ByVal dataToWrite() As String) As Integer Implements MfgControl.AdvancedHMI.Drivers.IComComponent.BeginWrite
        If dataToWrite Is Nothing Then
            Return 0
        End If

        Dim ElementCount As Integer = Math.Min(numberOfElemens, dataToWrite.Length)

        '* Convert Boolean string to values so converter can parse correctly as long as its not a string to write to
        If startAddress.IndexOf("ST", StringComparison.OrdinalIgnoreCase) < 0 Then
            For i = 0 To ElementCount - 1
                If String.Compare(dataToWrite(i), "True", True) = 0 Then
                    dataToWrite(i) = "1"
                End If
                If String.Compare(dataToWrite(i), "False", True) = 0 Then
                    dataToWrite(i) = "0"
                End If
            Next
        End If


        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, 1, GetProcessorType)

        If ParsedResult.FileType = 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Invalid Address")

        If startAddress.IndexOf("ST", StringComparison.OrdinalIgnoreCase) >= 0 Then
            '* Add an extra character to compensate for characters written in pairs to integers
            'Dim ConvertedData(dataToWrite.Length + 2 + 1) As Byte
            Dim ConvertedData(83) As Byte
            dataToWrite(0) &= Convert.ToChar(0)

            ConvertedData(0) = CByte(dataToWrite.Length - 1)
            Dim i As Integer = 2
            Dim StringBytes() As Byte = System.Text.Encoding.ASCII.GetBytes(dataToWrite(0))
            While i <= dataToWrite.Length
                ConvertedData(i + 1) = StringBytes(i - 2)
                ConvertedData(i) = StringBytes(i - 1)
                i += 2
            End While

            Return (WriteRawData(ParsedResult, 84, ConvertedData))
        ElseIf startAddress.IndexOf("F", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Dim SingleArray(ElementCount) As Single
            For i = 0 To SingleArray.Length - 1
                SingleArray(i) = CSng(dataToWrite(i))
            Next
            Return (Write(startAddress, ElementCount, SingleArray))
        Else
            Dim IntegerArray(ElementCount) As Int32
            For i = 0 To IntegerArray.Length - 1
                IntegerArray(i) = CInt(dataToWrite(i))
            Next
            Return (Write(startAddress, ElementCount, IntegerArray))
        End If
    End Function


    '**************************************************************
    '* Write to a PLC data file
    '*
    '**************************************************************
    Private Function WriteRawData(ByVal PAddressO As MfgControl.AdvancedHMI.Drivers.PCCCAddress, ByVal numberOfBytes As Integer, ByVal dataToWrite() As Byte) As Integer
        '* Create a clone to work with so we do not modifiy the original through a pointer
        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(ProcessorType)
        PAddress = DirectCast(PAddressO.Clone, MfgControl.AdvancedHMI.Drivers.PCCCAddress)

        '* Invalid address?
        If PAddress.FileType = 0 Then
            Return -5
        End If

        '**********************************************
        '* Use a bit level function if it is bit level
        '**********************************************
        Dim FunctionNumber As Byte

        Dim FilePosition, NumberOfBytesToWrite, DataStartPosition As Integer

        Dim reply As Integer

        Do While FilePosition < numberOfBytes AndAlso reply = 0
            '* Set next length of data to read. Max of 236 (slc 5/03 and up)
            '* This must limit to 82 for 5/02 and below
            If numberOfBytes - FilePosition < 164 Then
                NumberOfBytesToWrite = numberOfBytes - FilePosition
            Else
                NumberOfBytesToWrite = 164
            End If

            '* These files seem to be a special case
            If PAddress.FileType >= &HA1 And NumberOfBytesToWrite > &H78 Then
                NumberOfBytesToWrite = &H78
            End If

            Dim DataSize As Integer = NumberOfBytesToWrite  '+ PAddress.ByteStream.Length



            '* For now we are only going to allow one bit to be set/reset per call
            If PAddress.BitNumber < 16 Then DataSize = 8

            If PAddress.Element >= 255 Then DataSize += 2
            If PAddress.SubElement >= 255 Then DataSize += 2

            Dim DataW(DataSize - 1) As Byte

            ''* Byte Size
            'DataW(0) = ((NumberOfBytesToWrite And &HFF))
            ''* File Number
            'DataW(1) = (PAddress.FileNumber)
            ''* File Type
            'DataW(2) = (PAddress.FileType)
            ''* Starting Element Number
            'If PAddress.Element < 255 Then
            '    DataW(3) = (PAddress.Element)
            'Else
            '    DataW(5) = Math.Floor(PAddress.Element / 256)
            '    DataW(4) = PAddress.Element - (DataW(5) * 256) '*  calculate offset
            '    DataW(3) = 255
            'End If

            ''* Sub Element
            'If PAddress.SubElement < 255 Then
            '    DataW(DataW.Length - 1 - NumberOfBytesToWrite) = PAddress.SubElement
            'Else
            '    '* Use extended addressing
            '    DataW(DataW.Length - 1 - NumberOfBytesToWrite) = Math.Floor(PAddress.SubElement / 256)  '* 256+data(5)
            '    DataW(DataW.Length - 2 - NumberOfBytesToWrite) = PAddress.SubElement - (DataW(DataW.Length - 1 - NumberOfBytesToWrite) * 256) '*  calculate offset
            '    DataW(DataW.Length - 3 - NumberOfBytesToWrite) = 255
            'End If

            '* Are we changing a single bit?
            'If PAddress.BitNumber < 16 Then
            '    '* 23-SEP-12 - was missing an byte
            '    ReDim DataW(3)
            '    'PAddress.ByteStream.CopyTo(DataW, 0)

            '    FunctionNumber = &HAB  '* Ref http://www.iatips.com/pccc_tips.html#slc5_cmds
            '    '* Set the mask of which bit to change
            '    DataW(DataW.Length - 4) = CByte(Convert.ToInt32(Math.Pow(2, PAddress.BitNumber)) And &HFF)
            '    DataW(DataW.Length - 3) = CByte(Math.Pow(2, (PAddress.BitNumber - 8)))

            '    If dataToWrite(0) <= 0 Then
            '        '* Set bits to clear 
            '        DataW(DataW.Length - 2) = 0
            '        DataW(DataW.Length - 1) = 0
            '    Else
            '        '* Bits to turn on
            '        DataW(DataW.Length - 2) = CByte(Convert.ToInt32(Math.Pow(2, PAddress.BitNumber)) And &HFF)
            '        DataW(DataW.Length - 1) = CByte(Math.Pow(2, (PAddress.BitNumber - 8)))
            '    End If
            'Else
            DataStartPosition = DataW.Length - NumberOfBytesToWrite

            '* Prevent index out of range when numberToWrite exceeds dataToWrite.Length
            Dim ValuesToMove As Integer = NumberOfBytesToWrite - 1
            If ValuesToMove + FilePosition > dataToWrite.Length - 1 Then
                ValuesToMove = dataToWrite.Length - 1 - FilePosition
            End If


            Dim l As Integer = PAddress.ByteStream.Length - 1

            FunctionNumber = 0


            For i As Integer = 0 To ValuesToMove
                DataW(i) = dataToWrite(i + FilePosition)
            Next
            'End If

            Dim TNS As Integer = GetNextTNSNumber()
            Dim TNSLowerByte As Integer = TNS And 255
            'PAddress.InternallyRequested = InternalRequest
            'PAddress.TargetNode = m_TargetNode
            Requests(TNSLowerByte) = PAddress

            '************************************
            '* Is it a PLC5 Bit write? 08-MAY-12
            '************************************
            If PAddress.BitNumber < 16 Then
                FunctionNumber = &H26
                ReDim DataW(3)

                If dataToWrite(0) <= 0 Then
                    '* Clear the bit
                    '* AND mask
                    DataW(DataW.Length - 4) = CByte(255 - (Convert.ToInt32(2 ^ (PAddress.BitNumber)) And &HFF))
                    DataW(DataW.Length - 3) = CByte(255 - (Convert.ToInt32(2 ^ (PAddress.BitNumber - 8))))
                    '* OR Mask
                    DataW(DataW.Length - 2) = 0
                    DataW(DataW.Length - 1) = 0
                Else
                    '* Set the bit
                    '* AND Mask
                    DataW(DataW.Length - 4) = &HFF
                    DataW(DataW.Length - 3) = &HFF
                    '* OR Mask
                    DataW(DataW.Length - 2) = CByte(Convert.ToInt32(2 ^ (PAddress.BitNumber)) And &HFF)
                    DataW(DataW.Length - 1) = CByte(Convert.ToInt32(2 ^ (PAddress.BitNumber - 8)))
                End If
            Else
                'Dim DataBytes(dataToWrite.Length * 2 - 1) As Byte
                'For i = 0 To dataToWrite.Length - 1
                '    Array.ConstrainedCopy(BitConverter.GetBytes(Convert.ToInt16(dataToWrite)), 0, dataToWrite, i * 2, 2)
                'Next
                TNS = WordRangeWrite(PAddress, DataW, TNS)
            End If



            FilePosition += NumberOfBytesToWrite

            If PAddress.FileType <> &HA4 Then
                '* Use subelement because it works with all data file types
                PAddress.SubElement += Convert.ToInt32(NumberOfBytesToWrite / 2)
            Else
                '* Special case file - 28h bytes per elements
                PAddress.Element += Convert.ToInt32(NumberOfBytesToWrite / &H28)
            End If
        Loop

        If reply = 0 Then
            Return 0
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(DecodeMessage(reply))
        End If
    End Function
#End Region

#Region "Helper"
    Protected Overrides Sub OnResponseReceived(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If (e Is Nothing) Then Exit Sub

        Dim TNSLowerByte As Integer = e.TransactionNumber And 255

        If RawRequests(TNSLowerByte) Is Nothing Or e.OwnerObjectID <> MyObjectID Then
            Exit Sub
        End If

        'RawResponses(TNSLowerByte) = New MfgControl.AdvancedHMI.Drivers.PCCCReplyPacket(e.RawData)


        'If RawResponses(TNSLowerByte) IsNot Nothing AndAlso RawResponses(TNSLowerByte).Command = &H4F And Requests(TNSLowerByte) IsNot Nothing Then
        If RawResponses(TNSLowerByte) IsNot Nothing AndAlso Requests(TNSLowerByte) IsNot Nothing Then
            '**************************************************************
            '* Only extract and send back if this response contained data
            '**************************************************************
            Dim d() As String

            ''* TODO: find out why this is necessary
            'If (RawResponses(TNSLowerByte).EncapsulatedData.Count - 1) < 0 Then
            '    Dim dbg = 0
            'End If

            Dim ReturnedData(RawResponses(TNSLowerByte).EncapsulatedData.Count - 1) As Byte
            'If e.RawData.Length > 0 Then
            If (RawResponses(TNSLowerByte).EncapsulatedData IsNot Nothing) AndAlso (RawResponses(TNSLowerByte).EncapsulatedData.Count > 0) Then
                '***************************************************
                '* Extract returned data into appropriate data type
                '* Transfer block of data read to the data table array
                '***************************************************
                d = ExtractData(Requests(TNSLowerByte), RawResponses(TNSLowerByte).EncapsulatedData.ToArray())

                For i = 0 To d.Length - 1
                    e.Values.Add(d(i))
                Next
                Responses(TNSLowerByte) = e

                'Requests(TNSLowerByte).Responded = True

                OnDataReceived(e)

                '* was an error code returned?
            Else 'If DataPackets(TNSLowerByte).Count >= 7 Then
                '* TODO: Check STS byte and handle for asynchronous
                'If RawResponses(TNSLowerByte).EncapsulatedData(4) <> 0 Then
                'Responded(TNSLowerByte) = True
                'End If
            End If
        End If

        If Requests(TNSLowerByte) IsNot Nothing Then
            Requests(TNSLowerByte).Responded = True
        End If
    End Sub

    Protected Overridable Sub OnDataReceived(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If SynchronizingObject IsNot Nothing AndAlso SynchronizingObject.InvokeRequired Then
            Dim Parameters() As Object = {Me, e}
            SynchronizingObject.BeginInvoke(drsd, Parameters)
        Else
            'RaiseEvent DataReceived(Me, System.EventArgs.Empty)
            RaiseEvent DataReceived(Me, e)
        End If
    End Sub


    '****************************************************************************
    '* This is required to sync the event back to the parent form's main thread
    '****************************************************************************
    Private drsd As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs) = AddressOf DataReceivedSync
    'Private Sub DataReceivedSync(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Private Sub DataReceivedSync(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        RaiseEvent DataReceived(sender, e)
    End Sub

    '****************************************************
    '* Wait for a response from PLC before returning
    '****************************************************
    Private MaxTicks As Integer = 2000  '* 50 ticks per second
    Private Function WaitForResponse(ByVal rTNS As Integer) As Integer
        Dim Loops As Integer = 0
        Dim TNSLowerByte As Integer = rTNS And 255
        While Not Requests(TNSLowerByte).Responded And Loops < MaxTicks
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
#End Region

End Class