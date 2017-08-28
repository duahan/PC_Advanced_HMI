'**********************************************************************************************
'* AdvancedHMI Driver for Allen Bradley SLC/Micro Family
'* http://www.advancedhmi.com
'* PCCC Data Link Layer & Application Layer
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* support@advancedhmi.com
'* 04-MAR-15
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

'<Assembly: system.Security.Permissions.SecurityPermissionAttribute(system.Security.Permissions.SecurityAction.RequestMinimum)> 
'<Assembly: CLSCompliant(True)> 
Public MustInherit Class AllenBradleySLCMicro
    Inherits AllenBradleyPCCC
    Implements MfgControl.AdvancedHMI.Drivers.IComComponent

    Public Event DataReceived As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Public Event ComError As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Public Event DownloadProgress As EventHandler
    Public Event UploadProgress As EventHandler


    Private Responses(255) As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs
    Friend Requests(255) As MfgControl.AdvancedHMI.Drivers.PCCCAddress
    Protected waitHandle(255) As System.Threading.EventWaitHandle



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

        For index = 0 To 255
            waitHandle(index) = New System.Threading.EventWaitHandle(False, Threading.EventResetMode.AutoReset)
        Next
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
    Private m_ProcessorType As Integer = &H49
    Private ReadOnly Property ProcessorType As Integer
        Get
            Return GetProcessorType()
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
                    'Dim response As Integer
                    Dim Reply As Boolean
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
                                'response = WaitForResponse(TransactionNumber)
                                Reply = waitHandle(TransactionNumber And 255).WaitOne(2000)

                                Try
                                    If Reply And Not StopSubscriptions Then
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
                If e.ErrorId = 0 Then
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
                                If SynchronizingObject Is Nothing Then
                                    If SubscriptionList(i).dlgCallBack IsNot Nothing Then
                                        SubscriptionList(i).dlgCallBack.Invoke(Me, x)
                                    End If
                                ElseIf DirectCast(SynchronizingObject, Windows.Forms.Control).IsHandleCreated Then
                                    SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)
                                End If
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
                                If SynchronizingObject Is Nothing OrElse DirectCast(SynchronizingObject, Windows.Forms.Control).IsHandleCreated Then
                                    SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)
                                End If

                                'SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, New Object() {BitResult})
                            Else
                                '* All other data types
                                For k As Integer = 0 To SubscriptionList(i).NumberOfElements - 1
                                    BitResult(k) = d((SubscriptionList(i).Element - Requests(TNSLowerByte).Element + k))
                                Next

                                Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(BitResult, SubscriptionList(i).PLCAddress, e.TransactionNumber)
                                x.SubscriptionID = SubscriptionList(i).ID
                                Dim z() As Object = {Me, x}
                                If SynchronizingObject Is Nothing OrElse DirectCast(SynchronizingObject, Windows.Forms.Control).IsHandleCreated Then
                                    SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)
                                End If

                                'm_SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, d(SubscriptionList(i).PLCAddress.Element- PLCAddressByTNS(TNSReturned).Element))
                                'SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, New Object() {BitResult})

                            End If
                        End If
                    End If
                Else
                    '* Error to send
                    If (SubscriptionList(i).PLCAddress = Requests(TNSLowerByte).PLCAddress) Then
                        If SynchronizingObject IsNot Nothing Then
                            Dim z() As Object = {Me, e}
                            SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)
                        Else
                            SubscriptionList(i).dlgCallBack(Me, e)
                        End If
                    End If
                End If


                i += 1
            End While
        End SyncLock
    End Sub
#End Region

#Region "Public Methods"
    Public Structure DataFileDetails
        Dim FileType As String
        Dim FileNumber As Integer
        Dim NumberOfElements As Integer
    End Structure


    '*******************************************************************
    '* This is the start of reverse engineering to retreive data tables
    '*   Read 12 bytes File #0, Type 1, start at Element 21
    '*    Then extract the number of data and program files
    '*******************************************************************
    '* Retreives the list of data tables and number of elements in each
    Public Function GetDataMemory() As DataFileDetails()
        '**************************************************
        '* Read the File 0 (Program & data file directory
        '**************************************************
        Dim FileZeroData() As Byte = ReadFileDirectory()


        Dim NumberOfDataTables As Integer = FileZeroData(52) + FileZeroData(53) * 256
        Dim NumberOfProgramFiles As Integer = FileZeroData(46) + FileZeroData(47) * 256
        'Dim DataFiles(NumberOfDataTables - 1) As DataFileDetails
        Dim DataFiles As New System.Collections.ObjectModel.Collection(Of DataFileDetails)
        Dim FilePosition As Integer
        Dim BytesPerRow As Integer
        '*****************************************
        '* Process the data from the data table
        '*****************************************
        Select Case ProcessorType
            Case &H25, &H58 '*ML1000, SLC 5/02
                FilePosition = 93
                BytesPerRow = 8
            Case &H88 To &H9C   '* ML1100, ML1200, ML1500
                FilePosition = 103
                BytesPerRow = 10
            Case &H9F
                FilePosition = &H71
                BytesPerRow = 10
            Case Else               '* SLC 5/04, 5/05
                FilePosition = 79
                BytesPerRow = 10
        End Select


        '* Comb through data file 0 looking for data table definitions
        Dim i, k, BytesPerElement As Integer
        i = 0

        Dim DataFile As New DataFileDetails
        While k < NumberOfDataTables And FilePosition < FileZeroData.Length
            Select Case FileZeroData(FilePosition)
                Case &H82, &H8B : DataFile.FileType = "O"
                    BytesPerElement = 2
                Case &H83, &H8C : DataFile.FileType = "I"
                    BytesPerElement = 2
                Case &H84 : DataFile.FileType = "S"
                    BytesPerElement = 2
                Case &H85 : DataFile.FileType = "B"
                    BytesPerElement = 2
                Case &H86 : DataFile.FileType = "T"
                    BytesPerElement = 6
                Case &H87 : DataFile.FileType = "C"
                    BytesPerElement = 6
                Case &H88 : DataFile.FileType = "R"
                    BytesPerElement = 6
                Case &H89 : DataFile.FileType = "N"
                    BytesPerElement = 2
                Case &H8A : DataFile.FileType = "F"
                    BytesPerElement = 4
                Case &H8D : DataFile.FileType = "ST"
                    BytesPerElement = 84
                Case &H8E : DataFile.FileType = "A"
                    BytesPerElement = 2
                Case &H91 : DataFile.FileType = "L"   'Long Integer
                    BytesPerElement = 4
                Case &H92 : DataFile.FileType = "MG"   'Message Command 146
                    BytesPerElement = 50
                Case &H93 : DataFile.FileType = "PD"   'PID
                    BytesPerElement = 46
                Case &H94 : DataFile.FileType = "PLS"   'Programmable Limit Swith
                    BytesPerElement = 12

                Case Else : DataFile.FileType = "Undefined" '* 61h=Program File
                    BytesPerElement = 2
            End Select
            DataFile.NumberOfElements = Convert.ToInt32((FileZeroData(FilePosition + 1) + FileZeroData(FilePosition + 2) * 256) / BytesPerElement)
            DataFile.FileNumber = i

            '* Only return valid user data files
            If FileZeroData(FilePosition) > &H81 And FileZeroData(FilePosition) < &H9F Then
                DataFiles.Add(DataFile)
                'DataFile = New DataFileDetails
                k += 1
            End If

            '* Index file number once in the region of data files
            If k > 0 Then i += 1
            FilePosition += BytesPerRow
        End While

        '* Move to an array with a length of only good data files
        'Dim GoodDataFiles(k - 1) As DataFileDetails
        Dim GoodDataFiles(DataFiles.Count - 1) As DataFileDetails
        'For l As Integer = 0 To k - 1
        '    GoodDataFiles(l) = DataFiles(l)
        'Next

        DataFiles.CopyTo(GoodDataFiles, 0)

        Return GoodDataFiles
    End Function


    '*******************************************************************
    '*   Read the data file directory, File 0, Type 2
    '*    Then extract the number of data and program files
    '*******************************************************************
    'Private Function GetML1500DataMemory() As DataFileDetails()
    '    Dim reply As Integer
    '    Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress("D0:0", 1, ProcessorType)

    '    '* Get the length of File 0, Type 2. This is the program/data file directory
    '    'PAddress.FileNumber = 0
    '    'PAddress.FileType = 2
    '    'PAddress.Element = &H2F
    '    Dim data() As Byte = ReadRawData(PAddress).Values.ToArray


    '    If reply = 0 Then
    '        Dim FileZeroSize As Integer = data(0) + (data(1)) * 256

    '        'PAddress.Element = 0
    '        'PAddress.SubElement = 0
    '        '* Read all of File 0, Type 2
    '        PAddress.NumberOfElements = FileZeroSize / 2
    '        Dim FileZeroData() As Byte = ReadRawData(PAddress, reply)

    '        '* Start Reading the data table configuration
    '        Dim DataFiles(256) As DataFileDetails

    '        Dim FilePosition As Integer
    '        Dim i As Integer


    '        '* Process the data from the data table
    '        If reply = 0 Then
    '            '* Comb through data file 0 looking for data table definitions
    '            Dim k, BytesPerElement As Integer
    '            i = 0
    '            FilePosition = 143
    '            While FilePosition < FileZeroData.Length
    '                Select Case FileZeroData(FilePosition)
    '                    Case &H89 : DataFiles(k).FileType = "N"
    '                        BytesPerElement = 2
    '                    Case &H85 : DataFiles(k).FileType = "B"
    '                        BytesPerElement = 2
    '                    Case &H86 : DataFiles(k).FileType = "T"
    '                        BytesPerElement = 6
    '                    Case &H87 : DataFiles(k).FileType = "C"
    '                        BytesPerElement = 6
    '                    Case &H84 : DataFiles(k).FileType = "S"
    '                        BytesPerElement = 2
    '                    Case &H8A : DataFiles(k).FileType = "F"
    '                        BytesPerElement = 4
    '                    Case &H8D : DataFiles(k).FileType = "ST"
    '                        BytesPerElement = 84
    '                    Case &H8E : DataFiles(k).FileType = "A"
    '                        BytesPerElement = 2
    '                    Case &H88 : DataFiles(k).FileType = "R"
    '                        BytesPerElement = 6
    '                    Case &H82, &H8B : DataFiles(k).FileType = "O"
    '                        BytesPerElement = 2
    '                    Case &H83, &H8C : DataFiles(k).FileType = "I"
    '                        BytesPerElement = 2
    '                    Case &H91 : DataFiles(k).FileType = "L"   'Long Integer
    '                        BytesPerElement = 4
    '                    Case &H92 : DataFiles(k).FileType = "MG"   'Message Command 146
    '                        BytesPerElement = 50
    '                    Case &H93 : DataFiles(k).FileType = "PD"   'PID
    '                        BytesPerElement = 46
    '                    Case &H94 : DataFiles(k).FileType = "PLS"   'Programmable Limit Swith
    '                        BytesPerElement = 12

    '                    Case Else : DataFiles(k).FileType = "Undefined"  '* 61h=Program File
    '                        BytesPerElement = 2
    '                End Select
    '                DataFiles(k).NumberOfElements = (FileZeroData(FilePosition + 1) + FileZeroData(FilePosition + 2) * 256) / BytesPerElement
    '                DataFiles(k).FileNumber = i

    '                '* Only return valid user data files
    '                If FileZeroData(FilePosition) > &H81 And FileZeroData(FilePosition) < &H95 Then k += 1

    '                '* Index file number once in the region of data files
    '                If k > 0 Then i += 1
    '                FilePosition += 10
    '            End While

    '            '* Move to an array with a length of only good data files
    '            Dim GoodDataFiles(k - 1) As DataFileDetails
    '            For l As Integer = 0 To k - 1
    '                GoodDataFiles(l) = DataFiles(l)
    '            Next

    '            Return GoodDataFiles
    '        Else
    '            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(DecodeMessage(reply) & " - Failed to get data table list")
    '        End If
    '    Else
    '        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(DecodeMessage(reply) & " - Failed to get data table list")
    '    End If
    'End Function

    Private Function ReadFileDirectory() As Byte()
        GetProcessorType()

        '*****************************************************
        '* 1 & 2) Get the size of the File Directory
        '*****************************************************
        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress
        PAddress.ProcessorType = ProcessorType
        Select Case ProcessorType
            Case &H25, &H58  '* SLC 5/02 or ML1000
                'PAddress.FileType = 0
                'PAddress.Element = &H23
                PAddress.SetSpecial(0, &H23)
            Case &H88 To &H9C  '* ML1100, ML1200, ML1500
                'PAddress.FileType = 2
                'PAddress.Element = &H2F
                PAddress.SetSpecial(2, &H2F)
            Case &H9F           '*ML1400
                'PAddress.FileType = 3
                'PAddress.Element = &H34
                PAddress.SetSpecial(3, &H34)
            Case Else           '* SLC 5/04, SLC 5/05
                'PAddress.FileType = 1
                'PAddress.Element = &H23
                PAddress.SetSpecial(1, &H23)
        End Select

        Dim reply As Integer

        Dim TNS As Integer = ReadRawData(PAddress)
        Dim data() As Byte = Responses(TNS).RawData

        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Get Program Directory Size- " & DecodeMessage(reply))


        '*****************************************************
        '* 3) Read All of File 0 (File Directory)
        '*****************************************************
        PAddress.Element = 0
        Dim FileZeroSize As Integer = data(0) + data(1) * 256
        '* Deafult of 2 bytes per element in file 0
        PAddress.NumberOfElements = Convert.ToInt32(FileZeroSize / 2)
        TNS = ReadRawData(PAddress)
        Dim FileZeroData() As Byte = Responses(TNS And 255).RawData

        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Get Program Directory - " & DecodeMessage(reply))

        Return FileZeroData
    End Function
    '********************************************************************
    '* Retreive the ladder files
    '* This was developed from a combination of Chapter 12
    '*  and reverse engineering
    '********************************************************************
    Public Structure PLCFileDetails
        Dim FileType As Integer
        Dim FileNumber As Integer
        Dim NumberOfBytes As Integer
        Dim data() As Byte
    End Structure
    Public Function UploadProgramData() As System.Collections.ObjectModel.Collection(Of PLCFileDetails)
        ''*****************************************************
        ''* 1,2 & 3) Read all of the directory File
        ''*****************************************************
        Dim FileZeroData() As Byte = ReadFileDirectory()

        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(ProcessorType)
        Dim reply As Integer

        OnUploadProgress(System.EventArgs.Empty)

        '**************************************************
        '* 4) Parse the data from the File Directory data
        '**************************************************
        '*********************************************************************************
        '* Starting at corresponding File Position, each program is defined with 10 bytes
        '* 1st byte=File Type
        '* 2nd & 3rd bytes are program size
        '* 4th & 5th bytes are location with memory
        '*********************************************************************************
        Dim FilePosition As Integer
        Dim ProgramFile As New PLCFileDetails
        Dim ProgramFiles As New System.Collections.ObjectModel.Collection(Of PLCFileDetails)

        '*********************************************
        '* 4a) Add the directory information
        '*********************************************
        ProgramFile.FileNumber = 0
        ProgramFile.data = FileZeroData
        ProgramFile.FileType = PAddress.FileType
        ProgramFile.NumberOfBytes = FileZeroData.Length
        ProgramFiles.Add(ProgramFile)

        '**********************************************
        '* 5) Read the rest of the data tables
        '**********************************************
        Dim DataFileGroup, ForceFileGroup, SystemFileGroup, SystemLadderFileGroup As Integer
        Dim LadderFileGroup, Unknown1FileGroup, Unknown2FileGroup As Integer
        If reply = 0 Then
            Dim NumberOfProgramFiles As Integer = FileZeroData(46) + FileZeroData(47) * 256

            '* Comb through data file 0 and get the program file details
            Dim i As Integer
            '* The start of program file definitions
            Select Case ProcessorType
                Case &H25, &H58
                    FilePosition = 93
                Case &H88 To &H9C
                    FilePosition = 103
                Case &H9F   '* ML1400
                    FilePosition = &H71
                Case Else
                    FilePosition = 79
            End Select

            Do While FilePosition < FileZeroData.Length And reply = 0
                ProgramFile.FileType = FileZeroData(FilePosition)
                ProgramFile.NumberOfBytes = (FileZeroData(FilePosition + 1) + FileZeroData(FilePosition + 2) * 256)

                If ProgramFile.FileType >= &H40 AndAlso ProgramFile.FileType <= &H5F Then
                    ProgramFile.FileNumber = SystemFileGroup
                    SystemFileGroup += 1
                End If
                If (ProgramFile.FileType >= &H20 AndAlso ProgramFile.FileType <= &H3F) Then
                    ProgramFile.FileNumber = LadderFileGroup
                    LadderFileGroup += 1
                End If
                If (ProgramFile.FileType >= &H60 AndAlso ProgramFile.FileType <= &H7F) Then
                    ProgramFile.FileNumber = SystemLadderFileGroup
                    SystemLadderFileGroup += 1
                End If
                If ProgramFile.FileType >= &H80 AndAlso ProgramFile.FileType <= &H9F Then
                    ProgramFile.FileNumber = DataFileGroup
                    DataFileGroup += 1
                End If
                If ProgramFile.FileType >= &HA0 AndAlso ProgramFile.FileType <= &HBF Then
                    ProgramFile.FileNumber = ForceFileGroup
                    ForceFileGroup += 1
                End If
                If ProgramFile.FileType >= &HC0 AndAlso ProgramFile.FileType <= &HDF Then
                    ProgramFile.FileNumber = Unknown1FileGroup
                    Unknown1FileGroup += 1
                End If
                If ProgramFile.FileType >= &HE0 AndAlso ProgramFile.FileType <= &HFF Then
                    ProgramFile.FileNumber = Unknown2FileGroup
                    Unknown2FileGroup += 1
                End If

                'PAddress.FileType = ProgramFile.FileType
                'PAddress.FileNumber = ProgramFile.FileNumber
                'PAddress.BitNumber = 99   '* Do not let the extract data try to interpret bit level
                PAddress.SetSpecial(ProgramFile.FileType, ProgramFile.FileNumber)

                If ProgramFile.NumberOfBytes > 0 Then
                    PAddress.NumberOfElements = Convert.ToInt32(ProgramFile.NumberOfBytes / PAddress.BytesPerElement)
                    Dim TNS As Integer = ReadRawData(PAddress)
                    ProgramFile.data = Responses(TNS And 255).RawData
                    If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Read Program File " & PAddress.FileNumber & ", Type " & PAddress.FileType & " - " & DecodeMessage(reply))
                Else
                    Dim ZeroLengthData(-1) As Byte
                    ProgramFile.data = ZeroLengthData
                End If


                ProgramFiles.Add(ProgramFile)
                OnUploadProgress(System.EventArgs.Empty)

                i += 1
                '* 10 elements are used to define each program file
                '* SLC 5/02 or ML1000
                If ProcessorType = &H25 OrElse ProcessorType = &H58 Then
                    FilePosition += 8
                Else
                    FilePosition += 10
                End If
            Loop

        End If

        Return ProgramFiles
    End Function

    '****************************************************************
    '* Download a group of files defined in the PLCFiles Collection
    '****************************************************************
    Public Sub DownloadProgramData(ByVal PLCFiles As System.Collections.ObjectModel.Collection(Of PLCFileDetails))
        Dim TNS As Integer = GetNextTNSNumber()
        '******************************
        '* 1 & 2) Change to program Mode
        '******************************
        ChangeMode(Modes.Program, TNS)
        OnDownloadProgress(System.EventArgs.Empty)

        '*************************************************************************
        '* 2) Initialize Memory & Put in Download mode using Execute Command List
        '*************************************************************************
        Dim DataLength As Integer
        Select Case ProcessorType
            Case &H5B, &H78, &H6F
                DataLength = 13
            Case &H88 To &H9C
                DataLength = 15
            Case &H9F
                DataLength = 15  '*** TODO
            Case Else
                DataLength = 15
        End Select

        Dim data(DataLength) As Byte
        '* 2 commands
        data(0) = &H2
        '* Number of bytes in 1st command
        data(1) = &HA
        '* Function &HAA
        data(2) = &HAA
        '* Write 4 bytes
        data(3) = 4
        data(4) = 0
        '* File type 63
        data(5) = &H63

        '* Lets go ahead and setup the file type for later use
        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(ProcessorType)
        Dim reply As Integer

        '**********************************
        '* 2a) Search for File 0, Type 24
        '**********************************
        Dim i As Integer
        While i < PLCFiles.Count AndAlso (PLCFiles(i).FileNumber <> 0 OrElse PLCFiles(i).FileType <> &H24)
            i += 1
        End While

        '* Write bytes 02-07 from File 0, Type 24 to File 0, Type 63
        If i < PLCFiles.Count Then
            data(8) = PLCFiles(i).data(2)
            data(9) = PLCFiles(i).data(3)
            data(10) = PLCFiles(i).data(4)
            data(11) = PLCFiles(i).data(5)
            If DataLength > 14 Then
                data(12) = PLCFiles(i).data(6)
                data(13) = PLCFiles(i).data(7)
            End If
        End If


        Select Case ProcessorType
            Case &H78, &H5B, &H49, &H6F  '* SLC 5/05, 5/04, 5/03
                '* Read these 4 bytes to write back, File 0, Type 63
                PAddress.FileType = &H63
                PAddress.Element = 0
                PAddress.SubElement = 0
                PAddress.NumberOfElements = Convert.ToInt32(4 / PAddress.BytesPerElement)

                TNS = ReadRawData(PAddress)
                Dim FourBytes() As Byte = Responses(TNS And 255).RawData
                If reply = 0 Then
                    Array.Copy(FourBytes, 0, data, 8, 4)
                    PAddress.FileType = 1
                    PAddress.Element = &H23
                Else
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Read File 0, Type 63h - " & DecodeMessage(reply))
                End If

                '* Number of bytes in 1st command
                data(1) = &HA
                '* Number of bytes to write
                data(3) = 4
            Case &H88 To &H9C   '* ML1200, ML1500, ML1100
                '* Number of bytes in 1st command
                data(1) = &HC
                '* Number of bytes to write
                data(3) = 6
                PAddress.FileType = 2
                PAddress.Element = &H23
            Case &H9F   '* ML1400
                '* Number of bytes in 1st command
                data(1) = &HC       '* TODO
                '* Number of bytes to write
                data(3) = 6
                PAddress.FileType = 3
                PAddress.Element = &H28
            Case Else '* Fill in the gap for an unknown processor
                data(1) = &HA
                data(3) = 4
                PAddress.FileType = 1
                PAddress.Element = &H23
        End Select


        '* 1 byte in 2nd command - Start Download
        data(data.Length - 2) = 1
        data(data.Length - 1) = &H56

        '* TODO : convert to new PCCC
        'reply = PrefixAndSend(&HF, &H88, data, True, GetNextTNSNumber())
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Initialize for Download - " & DecodeMessage(reply))
        OnDownloadProgress(System.EventArgs.Empty)


        '*********************************
        '* 4) Secure Sole Access
        '*********************************
        TNS = GetEditResource()
        'reply = WaitForResponse(TNS)
        waitHandle(TNS).WaitOne(2000)
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Secure Sole Access - " & DecodeMessage(reply))
        OnDownloadProgress(System.EventArgs.Empty)

        '*********************************
        '* 5) Write the directory length
        '*********************************
        PAddress.BitNumber = 16
        Dim data3(1) As Byte
        data3(0) = CByte(PLCFiles(0).data.Length And &HFF)
        data3(1) = CByte((PLCFiles(0).data.Length - data3(0)) / 256)
        reply = WriteRawData(PAddress, 2, data3)
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Write Directory Length - " & DecodeMessage(reply))
        OnDownloadProgress(System.EventArgs.Empty)

        '*********************************
        '* 6) Write program directory
        '*********************************
        PAddress.Element = 0
        reply = WriteRawData(PAddress, PLCFiles(0).data.Length, PLCFiles(0).data)
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Write New Program Directory - " & DecodeMessage(reply))
        OnDownloadProgress(System.EventArgs.Empty)

        '*********************************
        '* 7) Write Program & Data Files
        '*********************************
        For i = 1 To PLCFiles.Count - 1
            PAddress.FileNumber = PLCFiles(i).FileNumber
            PAddress.FileType = PLCFiles(i).FileType
            PAddress.Element = 0
            PAddress.SubElement = 0
            PAddress.BitNumber = 16
            reply = WriteRawData(PAddress, PLCFiles(i).data.Length, PLCFiles(i).data)
            If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed when writing files to PLC - " & DecodeMessage(reply))
            OnDownloadProgress(System.EventArgs.Empty)
        Next

        '*********************************
        '* 8) Complete the Download
        '*********************************
        TNS = DownloadCompleted()
        'reply = WaitForResponse(TNS)
        waitHandle(TNS).WaitOne(2000)
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Indicate to PLC that Download is complete - " & DecodeMessage(reply))
        OnDownloadProgress(System.EventArgs.Empty)

        '*********************************
        '* 9) Release Sole Access
        '*********************************
        TNS = ReturnEditResource()
        ' reply = WaitForResponse(TNS)
        waitHandle(TNS).WaitOne(2000)
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Release Sole Access - " & DecodeMessage(reply))
        OnDownloadProgress(System.EventArgs.Empty)
    End Sub


    '* Get the number of slots in the rack
    Public Function GetSlotCount() As Integer
        '* Get the header of the data table definition file
        '* File 0, Type &H60, Element 0
        '* File Type (&H60 must be a system type), this was pulled from reverse engineering

        Dim TNS As Integer = GetNextTNSNumber()
        ProtectedTypeLogicalRead(4, 0, &H60, 0, 0, TNS)
        'Dim reply As Integer = WaitForResponse(TNS)
        Dim Result As Boolean = waitHandle(TNS).WaitOne(2000)

        If Result Then
            If RawResponses(TNS And 255).EncapsulatedData(6) > 0 Then
                Return RawResponses(TNS And 255).EncapsulatedData(6) - 1  '* if a rack based system, then subtract processor slot
            Else
                Return 0  '* micrologix reports 0 slots
            End If
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Release Sole Access - ") ' & DecodeMessage(reply))
        End If
    End Function

    Public Structure IOConfig
        Dim InputBytes As Integer
        Dim OutputBytes As Integer
        Dim CardCode As Integer
    End Structure
    '* Get IO card list currently in rack
    Public Function GetIOConfig() As IOConfig()
        Dim ProcessorType As Integer = GetProcessorType()


        If ProcessorType = &H89 Or ProcessorType = &H8C Then  '* Is it a Micrologix 1500?
            Return GetML1500IOConfig()
        Else
            Return GetSLCIOConfig()
        End If
    End Function

    '* Get IO card list currently in rack of a SLC
    Public Function GetSLCIOConfig() As IOConfig()
        Dim slots As Integer = GetSlotCount()

        If slots > 0 Then
            '****************************************
            '* Get the Slot 0(base unit) information
            '****************************************
            '* Get the header of the data table definition file
            '* Read File 0 to get the IO count on the base unit
            '* Get the header of the data table definition file
            '* x bytes File 0, Type &H60, Element 0
            '* File Type (&H60 must be a system type), this was pulled from reverse engineering
            Dim TNS As Integer = GetNextTNSNumber()
            ProtectedTypeLogicalRead(CByte(4 + (slots + 1) * 6 + 2), 0, &H60, 0, 0, TNS)
            ' Dim reply As Integer = WaitForResponse(TNS)
            Dim Result As Boolean = waitHandle(TNS).WaitOne(2000)


            Dim TNSLowerByte As Integer = TNS And 255

            Dim BytesForConverting(1) As Byte
            Dim IOResult(slots) As IOConfig
            If Result Then
                '* Extract IO information
                For i As Integer = 0 To slots
                    IOResult(i).InputBytes = RawResponses(TNSLowerByte).EncapsulatedData(i * 6 + 10)
                    IOResult(i).OutputBytes = RawResponses(TNSLowerByte).EncapsulatedData(i * 6 + 12)
                    BytesForConverting(0) = RawResponses(TNSLowerByte).EncapsulatedData(i * 6 + 14)
                    BytesForConverting(1) = RawResponses(TNSLowerByte).EncapsulatedData(i * 6 + 15)
                    IOResult(i).CardCode = BitConverter.ToInt16(BytesForConverting, 0)
                Next
                Return IOResult
            Else
                Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to get IO Config - ") ' & DecodeMessage(reply))
            End If
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to get Slot Count")
        End If
    End Function


    '* Get IO card list currently in rack of a ML1500
    Public Function GetML1500IOConfig() As IOConfig()
        '*************************************************************************
        '* Read the first 4 bytes of File 0, type 62 to get the total file length
        '**************************************************************************
        '****************************************
        '* Get the Slot 0(base unit) information
        '****************************************
        '* Read File 0 to get the IO count on the base unit
        '* Get the header of the data table definition file
        '* 4 bytes File 0, Type &H62, Element 0
        '* File Type (&H60 must be a system type), this was pulled from reverse engineering
        Dim TNS As Integer = GetNextTNSNumber()
        ProtectedTypeLogicalRead(4, 0, &H62, 0, 0, TNS)
        'Dim reply As Integer = WaitForResponse(TNS)
        Dim Result As Boolean = waitHandle(TNS).WaitOne(2000)

        Dim TNSLowerByte As Integer = TNS And 255



        '******************************************
        '* Read all of File Zero, Type 62
        '******************************************
        Dim data(4) As Byte
        If Result Then
            'TODO: Get this corrected
            Dim FileZeroSize As Integer = RawResponses(TNSLowerByte).EncapsulatedData(6) * 2
            Dim FileZeroData(FileZeroSize) As Byte
            Dim FilePosition As Integer
            Dim Subelement As Integer
            Dim i As Integer

            Dim ByteSize As Byte
            '* Number of bytes to read
            If FileZeroSize > &H50 Then
                ByteSize = &H50
            Else
                ByteSize = CByte(FileZeroSize)
            End If

            '* Loop through reading all of file 0 in chunks of 80 bytes
            Do While FilePosition < FileZeroSize And Result
                '****************************************
                '* Get the Slot 0(base unit) information
                '****************************************
                '* Read File 0 to get the IO count on the base unit
                '* Get the header of the data table definition file
                '* x bytes File 0, Type &H62, Element 0
                '* File Type (&H60 must be a system type), this was pulled from reverse engineering
                TNS = GetNextTNSNumber()
                ProtectedTypeLogicalRead(ByteSize, 0, &H62, 0, 0, TNS)
                'reply = WaitForResponse(TNS)
                Result = waitHandle(TNS).WaitOne(2000)

                '* Read the file
                TNSLowerByte = TNS And 255

                '* Transfer block of data read to the data table array
                i = 0
                Do While i < data(0)
                    FileZeroData(FilePosition) = RawResponses(TNSLowerByte).EncapsulatedData(i + 6)
                    i += 1
                    FilePosition += 1
                Loop


                '* point to the next element, by taking the last Start Element(in words) and adding it to the number of bytes read
                Subelement += Convert.ToInt32(data(0) / 2)
                If Subelement < 255 Then
                    data(3) = CByte(Subelement)
                Else
                    '* Use extended addressing
                    If data.Length < 6 Then ReDim Preserve data(5)
                    data(5) = CByte(Math.Floor(Subelement / 256))  '* 256+data(5)
                    data(4) = CByte(Subelement - (data(5) * 256)) '*  calculate offset
                    data(3) = 255
                End If

                '* Set next length of data to read. Max of 80
                If FileZeroSize - FilePosition < 80 Then
                    data(0) = CByte(FileZeroSize - FilePosition)
                Else
                    data(0) = 80
                End If
            Loop


            '**********************************
            '* Extract the data from the file
            '**********************************
            If Result Then
                Dim SlotCount As Integer = FileZeroData(2) - 2
                If SlotCount < 0 Then SlotCount = 0
                Dim SlotIndex As Integer = 1
                Dim IOResult(SlotCount) As IOConfig

                '*Start getting slot data
                i = 32 + SlotCount * 4
                Dim BytesForConverting(1) As Byte

                Do While SlotIndex <= SlotCount
                    IOResult(SlotIndex).InputBytes = FileZeroData(i + 2) * 2
                    IOResult(SlotIndex).OutputBytes = FileZeroData(i + 8) * 2
                    BytesForConverting(0) = FileZeroData(i + 18)
                    BytesForConverting(1) = FileZeroData(i + 19)
                    IOResult(SlotIndex).CardCode = BitConverter.ToInt16(BytesForConverting, 0)

                    i += 26
                    SlotIndex += 1
                Loop


                '****************************************
                '* Get the Slot 0(base unit) information
                '****************************************
                '* Read File 0 to get the IO count on the base unit
                '* Get the header of the data table definition file
                '* 8 bytes File 0, Type &H60, Element 0
                '* File Type (&H60 must be a system type), this was pulled from reverse engineering
                TNS = GetNextTNSNumber()
                TNS = ProtectedTypeLogicalRead(8, 0, &H60, 0, 0, TNS)
                'reply = WaitForResponse(TNS)
                Result = waitHandle(TNS).WaitOne(2000)


                If Result Then
                    TNSLowerByte = TNS And 255
                    IOResult(0).InputBytes = RawResponses(TNSLowerByte).EncapsulatedData(10)
                    IOResult(0).OutputBytes = RawResponses(TNSLowerByte).EncapsulatedData(12)
                Else
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to get Base IO Config for Micrologix 1500- ") ' & DecodeMessage(reply))
                End If


                Return IOResult
            End If
        End If

        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to get IO Config for Micrologix 1500- ") ' & DecodeMessage(reply))
    End Function
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

            If waitHandle(TNS).WaitOne(2000) Then
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
        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, numberOfElements, GetProcessorType)
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

            'Dim TNS As Integer = GetNextTNSNumber()
            'Requests(TNS) = ParsedResult


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
        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(ProcessorType)
        PAddress = DirectCast(PAddressO.Clone, MfgControl.AdvancedHMI.Drivers.PCCCAddress)

        Dim NumberOfBytesToRead, FilePosition As Integer
        Dim AccumulatedValues(PAddress.ByteSize - 1) As Byte
        Dim Result As Integer
        Dim TNS As Integer
        TNS = GetNextTNSNumber()

        '* Set an absoulute max bytes to read
        'NumberOfBytesToRead = 512
        Do While FilePosition < PAddress.ByteSize AndAlso Result = 0
            '* Set next length of data to read. Max of 236 (slc 5/03 and up)
            '* This must limit to 82 for 5/02 and below
            If PAddress.ByteSize - FilePosition < 236 Then
                NumberOfBytesToRead = PAddress.ByteSize - FilePosition
            Else
                NumberOfBytesToRead = 236
            End If

            NumberOfBytesToRead = Math.Min(NumberOfBytesToRead, MaximumPacketSize(PAddress.FileType))
            If NumberOfBytesToRead > 0 Then

                Dim DataSize As Integer
                'Dim Func As Byte

                'If PAddress.SubElement = 0 Then
                'DataSize = 3
                'Func = &HA1
                'Else

                DataSize = 4

                '**********************************************************************
                '* Link the TNS to the original address for use by the linked polling
                '**********************************************************************
                Dim TNSLowerByte As Integer = TNS And 255

                'PAddressO.TargetNode = m_TargetNode
                Requests(TNSLowerByte) = PAddressO
                waitHandle(TNSLowerByte).Reset()

                PAddress.ByteStream(0) = CByte(NumberOfBytesToRead)
                ProtectedTypeLogicalRead(Convert.ToByte(NumberOfBytesToRead), Convert.ToByte(PAddress.FileNumber), Convert.ToByte(PAddress.FileType), _
                                  Convert.ToByte(PAddress.Element), Convert.ToByte(PAddress.SubElement), TNS)

                Dim waitResult As Boolean = waitHandle(TNS And 255).WaitOne(2000)

                'Result = WaitForResponse(TNS)

                If waitResult Then
                    Result = 0
                    'If (FilePosition + NumberOfBytesToRead < PAddress.ByteSize) Then
                    'Result = WaitForResponse(TNSLowerByte)

                    '* Return status byte that came from controller
                    'If Result = 0 Then
                    If RawResponses(TNSLowerByte).EncapsulatedData IsNot Nothing Then
                        'If (RawResponses(TNSLowerByte).Status > 3) Then
                        Result = CInt(RawResponses(TNSLowerByte).Status)  '* STS position in DF1 message
                        '* If its and EXT STS, page 8-4
                        'If Result = &HF0 Then
                        '    '* The EXT STS is the last byte in the packet
                        '    'result = DataPackets(rTNS)(DataPackets(rTNS).Count - 2) + &H100
                        '    Result = RawResponses(TNSLowerByte).EncapsulatedData(RawResponses(TNSLowerByte).EncapsulatedData.Count - 1) + &H100
                        'End If
                        ' End If
                    Else
                        Result = -8 '* no response came back from PLC
                    End If
                    'End If

                    '***************************************************
                    '* Extract returned data into appropriate data type
                    '* Transfer block of data read to the data table array
                    '***************************************************
                    '* TODO: Check array bounds
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
                        ElseIf PAddress.FileType = &H8D Then
                            '* String type - version 3.99a
                            PAddress.Element += Convert.ToInt32(NumberOfBytesToRead / 84)
                        Else
                            '* Use subelement because it works with all data file types
                            PAddress.SubElement += Convert.ToInt32(NumberOfBytesToRead / 2)
                        End If
                    End If
                Else
                    Responses(TNS And 255) = New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(Result, "Error")
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
        waitHandle(TNSLowerByte2).Set()

        Return TNS
    End Function

    Private Function MaximumPacketSize(ByVal fileType As Integer) As Integer
        Dim Result As Integer = 236

        '* The SLC 5/02 can only read &H50 bytes per read, possibly the ML1500
        'If NumberOfBytesToRead > &H50 AndAlso (ProcessorType = &H25 Or ProcessorType = &H89) Then
        If (ProcessorType = &H25) Then
            Result = &H50
        End If

        '* String is an exception
        If fileType = &H8D Then
            '* Only two string elements can be read on each read (168 bytes)
            Result = 168
        End If

        If (fileType = &H86 OrElse fileType = &H87) Then
            '* Timers & counters read in multiples of 6 bytes
            Result = 234
        End If

        '* Data Monitor File is an exception
        If fileType = &HA4 Then
            '* Only two string elements can be read on each read (168 bytes)
            Result = &H78
        End If

        Return Result
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
        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, numberOfElements, GetProcessorType)

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

        '* Convert Boolean string to values so converter can parse correctly as long as its not a string to write to
        If startAddress.IndexOf("ST", StringComparison.OrdinalIgnoreCase) < 0 Then
            If String.Compare(dataToWrite, "True", True) = 0 Then
                dataToWrite = "1"
            End If
            If String.Compare(dataToWrite, "False", True) = 0 Then
                dataToWrite = "0"
            End If
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

    '* Write a String
    '* Write a string value to a string data table
    '* The startAddress is in the common form of AB addressing (e.g. ST9:0)
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

            Dim DataSize As Integer = NumberOfBytesToWrite '+ PAddress.ByteStream.Length

            '* Is it a PLC5?
            If MfgControl.AdvancedHMI.Drivers.PCCCAddress.IsPLC5(ProcessorType) Then
                DataSize -= 1
            End If


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


            'PAddress.ByteStream.CopyTo(DataW, 0)

            '**** PATCH - Set up to read 3 elements of timer,counters, etc, but it affected writing
            'If DataW(0) = dataToWrite.Length * 3 Then
            'DataW(0) = CByte(dataToWrite.Length)
            'If PAddress.PLCAddress.IndexOf(".PRE", 0, System.StringComparison.CurrentCultureIgnoreCase) > 0 Then
            '    DataW(4) = 1
            'End If
            'If PAddress.PLCAddress.IndexOf(".ACC", 0, System.StringComparison.CurrentCultureIgnoreCase) > 0 Then
            '    DataW(4) = 2
            'End If
            'End If
            '***************************************************************************************************

            Dim l As Integer = PAddress.ByteStream.Length

            '* Is it a PLC5?
            If False And MfgControl.AdvancedHMI.Drivers.PCCCAddress.IsPLC5(ProcessorType) Then
                FunctionNumber = 0
                l -= 1
            Else
                l = 0
                FunctionNumber = &HAA
            End If


            For i As Integer = 0 To ValuesToMove
                DataW(i + l) = dataToWrite(i + FilePosition)
            Next
            'End If

            Dim TNS As Integer = GetNextTNSNumber()
            Dim TNSLowerByte As Integer = TNS And 255
            'PAddress.InternallyRequested = InternalRequest
            'PAddress.TargetNode = m_TargetNode
            Requests(TNSLowerByte) = PAddress

            If PAddress.BitNumber < 16 Then
                Dim BitMaskData(3) As Byte

                '* Set the mask of which bit to change
                BitMaskData(0) = CByte(Convert.ToInt32(Math.Pow(2, PAddress.BitNumber)) And &HFF)
                BitMaskData(1) = CByte(Math.Pow(2, (PAddress.BitNumber - 8)))

                If dataToWrite(0) <= 0 Then
                    '* Set bits to clear 
                    BitMaskData(2) = 0
                    BitMaskData(3) = 0
                Else
                    '* Bits to turn on
                    BitMaskData(2) = CByte(Convert.ToInt32(Math.Pow(2, PAddress.BitNumber)) And &HFF)
                    BitMaskData(3) = CByte(Math.Pow(2, (PAddress.BitNumber - 8)))
                End If

                TNS = ProtectedTypeLogicalMaskedBitWrite(Convert.ToByte(2), Convert.ToByte(PAddress.FileNumber), _
                               Convert.ToByte(PAddress.FileType), Convert.ToByte(PAddress.Element), Convert.ToByte(PAddress.SubElement), _
                              BitMaskData, TNS)
            Else
                ProtectedTypeLogicalWrite(Convert.ToByte(DataW.Length), Convert.ToByte(PAddress.FileNumber), _
                                              Convert.ToByte(PAddress.FileType), Convert.ToByte(PAddress.Element), Convert.ToByte(PAddress.SubElement), _
                                             DataW, TNS)
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
    Protected Overridable Sub OnDownloadProgress(ByVal e As System.EventArgs)
        RaiseEvent DownloadProgress(Me, e)
    End Sub

    Protected Overridable Sub OnUploadProgress(ByVal e As System.EventArgs)
        RaiseEvent DownloadProgress(Me, e)
    End Sub


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
            Else
                Dim dbg = 0
                'If DataPackets(TNSLowerByte).Count >= 7 Then
                '* TODO: Check STS byte and handle for asynchronous
                'If RawResponses(TNSLowerByte).EncapsulatedData(4) <> 0 Then
                'Responded(TNSLowerByte) = True
                'End If
            End If
        End If

        If Requests(TNSLowerByte) IsNot Nothing Then
            Requests(TNSLowerByte).Responded = True
            waitHandle(TNSLowerByte).Set()
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

    '***************************************************************************************
    '* If an error comes back from the driver, return the description back to the control
    '***************************************************************************************
    Protected Sub DataLinkLayerComError(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        Dim d() As String = {MfgControl.AdvancedHMI.Drivers.CIPUtilities.DecodeCIPError(e.ErrorId)}


        If Requests(e.TransactionNumber And 255) IsNot Nothing Then
            Responses(e.TransactionNumber And 255) = e

            'Requests(e.TransactionNumber And 255)(0).Responded = True
            If waitHandle(e.TransactionNumber And 255) IsNot Nothing Then
                waitHandle(e.TransactionNumber And 255).Set()
            End If
        End If

        OnComError(e)

        SendToSubscriptions(e)
    End Sub

    Protected Overridable Sub OnComError(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If SynchronizingObject IsNot Nothing AndAlso SynchronizingObject.InvokeRequired Then
            Dim Parameters() As Object = {Me, e}
            SynchronizingObject.BeginInvoke(drsd, Parameters)
        Else
            'RaiseEvent DataReceived(Me, System.EventArgs.Empty)
            RaiseEvent ComError(Me, e)
        End If
    End Sub

    '****************************************************************************
    '* This is required to sync the event back to the parent form's main thread
    '****************************************************************************
    Private cesd As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs) = AddressOf ComErrorSync
    'Private Sub DataReceivedSync(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Private Sub ComErrorSync(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        RaiseEvent ComError(sender, e)
    End Sub




    ''****************************************************
    ''* Wait for a response from PLC before returning
    ''****************************************************
    'Private MaxTicks As Integer = 2000  '* 50 ticks per second
    'Private Function WaitForResponse(ByVal rTNS As Integer) As Integer
    '    Dim Loops As Integer = 0
    '    Dim TNSLowerByte As Integer = rTNS And 255
    '    While Not Requests(TNSLowerByte).Responded And Loops < MaxTicks
    '        System.Threading.Thread.Sleep(2)
    '        Loops += 1
    '    End While


    '    If Loops >= MaxTicks Then
    '        Return -20
    '        'ElseIf DLL(MyDLLInstance).LastResponseWasNAK Then
    '        '    Return -21
    '    End If

    '    Return 0
    'End Function
#End Region

End Class



'*********************************************************************************
'* This is used for linking a notification.
'* An object can request a continuous poll and get a callback when value updated
'*********************************************************************************
Friend Class PCCCSubscription
    Inherits MfgControl.AdvancedHMI.Drivers.PCCCAddress

#Region "Constructors"
    '* 22-NOV-12 changed to shared
    Private Shared CurrentID As Integer
    Public Sub New()
        CurrentID += 1
        m_ID = CurrentID
    End Sub

    Public Sub New(ByVal PLCAddress As String, ByVal NumberOfElements As Integer, ByVal ProcessorType As Integer)
        MyBase.New(PLCAddress, NumberOfElements, ProcessorType)
        '* 22-NOV-12 added
        CurrentID += 1
        m_ID = CurrentID
    End Sub
#End Region

#Region "Properties"
    Public dlgCallBack As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)

    'Public Property dlgCallBack As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Public Property PollRate As Integer

    Private m_ID As Integer
    Public ReadOnly Property ID As Integer
        Get
            Return m_ID
        End Get
    End Property

    'Public Property ElementsToRead As Integer
#End Region
End Class

