﻿'****************************************************************************
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* support@advancedhmi.com
'* 12-JUN-11
'*
'* Copyright 2011 Archie Jacobs
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
'* 12-JUN-11 Created
'* 31-DEC-11 Added BooleanDisplay property
'* 28-SEP-12 Catch specific PLCDriverException when trying to subscribe
'* 29-JAN-13 Added KeypadMinValue and KeypadMaxValue
'* 10-JUL-13 Added Value property
'****************************************************************************
Public Class BasicLabel
    Inherits System.Windows.Forms.Label

    Public Event ValueChanged As EventHandler


#Region "Basic Properties"
    Private SavedBackColor As System.Drawing.Color

    '* Remove Text from the property window so users do not attempt to use it
    <System.ComponentModel.Browsable(False)> _
    Public Overrides Property Text As String
        Get
            Return MyBase.Text
        End Get
        Set(value As String)
            MyBase.Text = value
        End Set
    End Property

    '******************************************************************************************
    '* Use the base control's text property and make it visible as a property on the designer
    '******************************************************************************************
    Private m_Value As String
    Public Property Value As String
        Get
            Return m_Value
        End Get
        Set(ByVal value As String)
            If value <> m_Value Then
                If value IsNot Nothing Then
                    m_Value = value
                    UpdateText()
                 Else
                    m_Value = ""
                    MyBase.Text = ""
                End If
                '* Be sure error handler doesn't revert back to an incorrect text
                OriginalText = MyBase.Text

                OnvalueChanged(EventArgs.Empty)
            End If
        End Set
    End Property

    Private m_ValueLeftPadCharacter As Char
    Public Property ValueLeftPadCharacter() As Char
        Get
            Return m_ValueLeftPadCharacter
        End Get
        Set(ByVal value As Char)
            m_ValueLeftPadCharacter = value
            UpdateText()
            Invalidate()
        End Set
    End Property

    Private m_ValueLeftPadLength As Integer
    Public Property ValueLeftPadLength As Integer
        Get
            Return m_ValueLeftPadLength
        End Get
        Set(ByVal value As Integer)
            m_ValueLeftPadLength = value
            UpdateText()
            Invalidate()
        End Set
    End Property


    '**********************************
    '* Prefix and suffixes to text
    '**********************************
    Private m_Prefix As String
    Public Property ValuePrefix() As String
        Get
            Return m_Prefix
        End Get
        Set(ByVal value As String)
            m_Prefix = value
            UpdateText()
            Invalidate()
        End Set
    End Property

    Private m_Suffix As String
    Public Property ValueSuffix() As String
        Get
            Return m_Suffix
        End Get
        Set(ByVal value As String)
            m_Suffix = value
            UpdateText()
            Invalidate()
        End Set
    End Property


    '***************************************************************
    '* Property - Highlight Color
    '***************************************************************
    Private _Highlightcolor As Drawing.Color = Drawing.Color.Red
    <System.ComponentModel.Category("Appearance")> _
    Public Property HighlightColor() As Drawing.Color
        Get
            Return _Highlightcolor
        End Get
        Set(ByVal value As Drawing.Color)
            _Highlightcolor = value
        End Set
    End Property

    Private _HighlightKeyChar As String = "!"
    <System.ComponentModel.Category("Appearance")> _
    Public Property HighlightKeyCharacter() As String
        Get
            Return _HighlightKeyChar
        End Get
        Set(ByVal value As String)
            _HighlightKeyChar = value
        End Set
    End Property


    Private m_Format As String
    Public Property NumericFormat() As String
        Get
            Return m_Format
        End Get
        Set(ByVal value As String)
            m_Format = value
        End Set
    End Property

    Private m_ValueScaleFactor As Double = 1
    Public Property ValueScaleFactor() As Double
        Get
            Return m_ValueScaleFactor
        End Get
        Set(ByVal value As Double)
            m_ValueScaleFactor = value
            'TODO: Does not refresh in designmode
            'Text = MyBase.Text
        End Set
    End Property

    Public Enum BooleanDisplayOption
        TrueFalse
        YesNo
        OnOff
    End Enum

    Private m_BooleanDisplay As BooleanDisplayOption
    Public Property BooleanDisplay() As BooleanDisplayOption
        Get
            Return m_BooleanDisplay
        End Get
        Set(ByVal value As BooleanDisplayOption)
            m_BooleanDisplay = value
        End Set
    End Property
#End Region

#Region "PLC Related Properties"
    '*****************************************************
    '* Property - Component to communicate to PLC through
    '*****************************************************
    Private m_CommComponent As MfgControl.AdvancedHMI.Drivers.IComComponent
    <System.ComponentModel.Description("Driver Instance for data reading and writing")> _
    <System.ComponentModel.Category("PLC Properties")> _
    Public Property CommComponent()  As MfgControl.AdvancedHMI.Drivers.IComComponent
        Get
            Return m_CommComponent
        End Get
        Set(ByVal value As  MfgControl.AdvancedHMI.Drivers.IComComponent)
            If m_CommComponent IsNot value Then
                If SubScriptions IsNot Nothing Then
                    SubScriptions.UnsubscribeAll()
                End If

                m_CommComponent = value

                SubscribeToComDriver()
            End If
        End Set
    End Property

    Private _PollRate As Integer
    Public Property PollRate() As Integer
        Get
            Return _PollRate
        End Get
        Set(ByVal value As Integer)
            _PollRate = value
        End Set
    End Property

    Private m_KeypadText As String
    Public Property KeypadText() As String
        Get
            Return m_KeypadText
        End Get
        Set(ByVal value As String)
            m_KeypadText = value
        End Set
    End Property

    Private m_KeypadFont As Font = New Font("Arial", 10)
    Public Property KeypadFont() As Font
        Get
            Return m_KeypadFont
        End Get
        Set(ByVal value As Font)
            m_KeypadFont = value
        End Set
    End Property

    Private m_KeypadForeColor As Color = Color.WhiteSmoke
    Public Property KeypadFontColor() As Color
        Get
            Return m_KeypadForeColor
        End Get
        Set(ByVal value As Color)
            m_KeypadForeColor = value
        End Set
    End Property

    Private m_KeypadWidth As Integer = 300
    Public Property KeypadWidth() As Integer
        Get
            Return m_KeypadWidth
        End Get
        Set(ByVal value As Integer)
            m_KeypadWidth = value
        End Set
    End Property

    '* 29-JAN-13
    Private m_KeypadMinValue As Double
    Public Property KeypadMinValue As Double
        Get
            Return m_KeypadMinValue
        End Get
        Set(value As Double)
            m_KeypadMinValue = value
        End Set
    End Property

    Private m_KeypadMaxValue As Double
    Public Property KeypadMaxValue As Double
        Get
            Return m_KeypadMaxValue
        End Get
        Set(value As Double)
            m_KeypadMaxValue = value
        End Set
    End Property

    Private m_KeypadScaleFactor As Double = 1
    <System.ComponentModel.DefaultValue(1)> _
    Public Property KeypadScaleFactor() As Double
        Get
            Return m_KeypadScaleFactor
        End Get
        Set(ByVal value As Double)
            m_KeypadScaleFactor = value
        End Set
    End Property

    Private m_KeypadAlphaNumeric As Boolean
    Property KeypadAlpahNumeric As Boolean
        Get
            Return m_KeypadAlphaNumeric
        End Get
        Set(value As Boolean)
            m_KeypadAlphaNumeric = value
        End Set
    End Property

    Private m_KeypadShowCurrentValue As Boolean
    Property KeypadShowCurrentValue As Boolean
        Get
            Return m_KeypadShowCurrentValue
        End Get
        Set(value As Boolean)
            m_KeypadShowCurrentValue = value
        End Set
    End Property


    Private m_SuppressErrorDisplay As Boolean
    <System.ComponentModel.DefaultValue(False)> _
    Public Property SuppressErrorDisplay As Boolean
        Get
            Return m_SuppressErrorDisplay
        End Get
        Set(value As Boolean)
            m_SuppressErrorDisplay = value
        End Set
    End Property



    '*****************************************
    '* Property - Address in PLC to Link to
    '*****************************************
    Private m_PLCAddressValue As String = ""
    <System.ComponentModel.DefaultValue("")> _
    <System.ComponentModel.Category("PLC Properties")> _
    Public Property PLCAddressValue() As String
        Get
            Return m_PLCAddressValue
        End Get
        Set(ByVal value As String)
            If m_PLCAddressValue <> value Then
                m_PLCAddressValue = value

                '* When address is changed, re-subscribe to new address
                SubscribeToComDriver()
            End If
        End Set
    End Property

    'Private m_PLCAddressValue2 As New MfgControl.AdvancedHMI.Drivers.PLCAddressItem
    '<System.ComponentModel.Category("PLC Properties")> _
    'Public Property PLCAddressValue2() As MfgControl.AdvancedHMI.Drivers.PLCAddressItem
    '    Get
    '        Return m_PLCAddressValue2
    '    End Get
    '    Set(value As MfgControl.AdvancedHMI.Drivers.PLCAddressItem)
    '        m_PLCAddressValue2 = value
    '    End Set
    'End Property

    '*****************************************
    '* Property - Address in PLC to Link to
    '*****************************************
    Private m_PLCAddressVisible As String = ""
    <System.ComponentModel.DefaultValue("")> _
    <System.ComponentModel.Category("PLC Properties")> _
    Public Property PLCAddressVisible() As String
        Get
            Return m_PLCAddressVisible
        End Get
        Set(ByVal value As String)
            If m_PLCAddressVisible <> value Then
                m_PLCAddressVisible = value

                '* When address is changed, re-subscribe to new address
                SubscribeToComDriver()
            End If
        End Set
    End Property

#End Region

#Region "Private Methods"
    Private Sub UpdateText()
        If m_Value IsNot Nothing Then
            '* True/False comes from driver, change if BooleanDisplay is different 31-DEC-11
            If (m_Value = "True" Or m_Value = "False") And m_BooleanDisplay <> BooleanDisplayOption.TrueFalse Then
                If Value = "True" Then
                    If m_BooleanDisplay = BooleanDisplayOption.OnOff Then MyBase.Text = "On"
                    If m_BooleanDisplay = BooleanDisplayOption.YesNo Then MyBase.Text = "Yes"
                Else
                    If m_BooleanDisplay = BooleanDisplayOption.OnOff Then MyBase.Text = "Off"
                    If m_BooleanDisplay = BooleanDisplayOption.YesNo Then MyBase.Text = "No"
                End If
            End If


            If (m_Format IsNot Nothing AndAlso (String.Compare(m_Format, "") <> 0)) And (Not DesignMode) Then
                Try
                    '* 31-MAY-13, 17-JUN-15 Changed from Single to Double to prevent rounding problems
                    Dim v As Double
                    If Double.TryParse(m_Value, v) Then
                        MyBase.Text = m_Prefix & (CDbl(v) * m_ValueScaleFactor).ToString(m_Format) & m_Suffix
                    End If
                Catch exC As InvalidCastException
                    If MyBase.Text = "Check NumericFormat and variable type" Then
                        MyBase.Text = "----"
                    Else
                        MyBase.Text = Value
                    End If
                Catch ex As Exception
                    MyBase.Text = "Check NumericFormat and variable type"
                End Try
            Else
                '* Highlight in red if a Highlightcharacter found mark is in text
                If Value.IndexOf(_HighlightKeyChar) >= 0 Then
                    If MyBase.BackColor <> _Highlightcolor Then SavedBackColor = MyBase.BackColor
                    MyBase.BackColor = _Highlightcolor
                Else
                    If SavedBackColor <> Nothing Then MyBase.BackColor = SavedBackColor
                End If

                If m_ValueScaleFactor = 1 Then
                    If m_ValueLeftPadLength <= 0 Then
                        If m_Prefix IsNot Nothing AndAlso m_Prefix.Length > 0 Then
                            MyBase.Text = m_Prefix & m_Value & m_Suffix
                        ElseIf m_Suffix IsNot Nothing AndAlso m_Suffix.Length > 0 Then
                            MyBase.Text = m_Value & m_Suffix
                        Else
                            MyBase.Text = m_Value
                        End If
                    Else
                        MyBase.Text = m_Prefix & m_Value.PadLeft(m_ValueLeftPadLength, m_ValueLeftPadCharacter) & m_Suffix
                    End If
                Else
                    Try
                        MyBase.Text = m_Prefix & CStr(Value * m_ValueScaleFactor) & m_Suffix
                    Catch ex As Exception
                        DisplayError("Scale Factor Error - " & ex.Message)
                    End Try

                End If
            End If
        Else
            MyBase.Text = ""
        End If
    End Sub
#End Region

#Region "Events"
    '********************************************************************
    '* When an instance is added to the form, set the comm component
    '* property. If a comm component does not exist, add one to the form
    '********************************************************************
    Protected Overrides Sub OnCreateControl()
        MyBase.OnCreateControl()

        If Me.DesignMode Then
            '********************************************************
            '* Search for AdvancedHMIDrivers.IComComponent component
            '*   in the Designer Host Container
            '* If one exists, set the client of this component to it
            '********************************************************
            Dim i As Integer
            While m_CommComponent Is Nothing And i < Me.Site.Container.Components.Count
                If Me.Site.Container.Components(i).GetType.GetInterface("IComComponent") IsNot Nothing Then m_CommComponent = Me.Site.Container.Components(i)
                i += 1
            End While
        Else
            SubscribeToComDriver()
        End If
    End Sub

    Public Sub New()
        MyBase.new()

        Value = "BasicLabel"


        If (MyBase.ForeColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.ControlText) Or ForeColor = Color.FromArgb(0, 0, 0)) Then
            ForeColor = System.Drawing.Color.WhiteSmoke
        End If
    End Sub

    Protected Overrides Sub OnHandleCreated(e As System.EventArgs)
        MyBase.OnHandleCreated(e)

        'If ForeColor.R = Me.Parent.BackColor.R And ForeColor.G = Me.Parent.BackColor.G And ForeColor.B = Me.Parent.BackColor.B Then
        '    ForeColor = Drawing.Color.FromArgb(Not Me.ForeColor.R, Not Me.ForeColor.G, Not Me.ForeColor.B)
        'End If
    End Sub

    '****************************************************************
    '* UserControl overrides dispose to clean up the component list.
    '****************************************************************
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing Then
                If SubScriptions IsNot Nothing Then
                    SubScriptions.dispose()
                End If
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Protected Overridable Sub OnvalueChanged(ByVal e As EventArgs)
        RaiseEvent ValueChanged(Me, e)
    End Sub
#End Region

#Region "Subscribing and PLC data receiving"
    Private SubScriptions As SubscriptionHandler
    '*******************************************************************************
    '* Subscribe to addresses in the Comm(PLC) Driver
    '* This code will look at properties to find the "PLCAddress" + property name
    '*
    '*******************************************************************************
    Private Sub SubscribeToComDriver()
        If Not DesignMode And IsHandleCreated Then
            '* Create a subscription handler object
            If SubScriptions Is Nothing Then
                SubScriptions = New SubscriptionHandler
                SubScriptions.CommComponent = m_CommComponent
                SubScriptions.Parent = Me
                AddHandler SubScriptions.DisplayError, AddressOf DisplaySubscribeError
            End If

            SubScriptions.SubscribeAutoProperties()
        End If
    End Sub

    '***************************************
    '* Call backs for returned data
    '***************************************
    Private OriginalText As String
    Private Sub PolledDataReturned(ByVal sender As Object, ByVal e As SubscriptionHandlerEventArgs)
    End Sub

#End Region

#Region "Error Display"
    Private Sub DisplaySubscribeError(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        DisplayError(e.ErrorMessage)
    End Sub

    '********************************************************
    '* Show an error via the text property for a short time
    '********************************************************
    Private ErrorDisplayTime As System.Windows.Forms.Timer
    Private ErrorLock As New Object
    Private Sub DisplayError(ByVal ErrorMessage As String)
        If Not m_SuppressErrorDisplay Then
            '* Create the error display timer
            If ErrorDisplayTime Is Nothing Then
                ErrorDisplayTime = New System.Windows.Forms.Timer
                AddHandler ErrorDisplayTime.Tick, AddressOf ErrorDisplay_Tick
                ErrorDisplayTime.Interval = 5000
            End If

            '* Save the text to return to
            SyncLock (ErrorLock)
                If Not ErrorDisplayTime.Enabled Then
                    ErrorDisplayTime.Enabled = True
                    OriginalText = MyBase.Text
                    MyBase.Text = ErrorMessage
                End If
            End SyncLock
        End If
    End Sub


    '**************************************************************************************
    '* Return the text back to its original after displaying the error for a few seconds.
    '**************************************************************************************
    Private Sub ErrorDisplay_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs)
        'UpdateText()
        SyncLock (ErrorLock)
            MyBase.Text = OriginalText
            'If ErrorDisplayTime IsNot Nothing Then
            ErrorDisplayTime.Enabled = False
            ' ErrorIsDisplayed = False
        End SyncLock
        'RemoveHandler ErrorDisplayTime.Tick, AddressOf ErrorDisplay_Tick
        'ErrorDisplayTime.Dispose()
        'ErrorDisplayTime = Nothing
        'End If
    End Sub
#End Region

#Region "Keypad popup for data entry"
    '*****************************************
    '* Property - Address in PLC to Write Data To
    '*****************************************
    Private m_PLCAddressKeypad As String = ""
    <System.ComponentModel.Category("PLC Properties")> _
    Public Property PLCAddressKeypad() As String
        Get
            Return m_PLCAddressKeypad
        End Get
        Set(ByVal value As String)
            If m_PLCAddressKeypad <> value Then
                m_PLCAddressKeypad = value
            End If
        End Set
    End Property

    Private WithEvents KeypadPopUp As MfgControl.AdvancedHMI.Controls.IKeyboard

    Private Sub KeypadPopUp_ButtonClick(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Controls.KeyPadEventArgs) Handles KeypadPopUp.ButtonClick
        If e.Key = "Quit" Then
            KeypadPopUp.Visible = False
        ElseIf e.Key = "Enter" Then
            If m_CommComponent Is Nothing Then
                DisplayError("CommComponent Property not set")
            Else
                If KeypadPopUp.Value IsNot Nothing AndAlso (String.Compare(KeypadPopUp.Value, "") <> 0) Then
                    '* 29-JAN-13 - Validate value if a Min/Max was specified
                    Try
                        If m_KeypadMaxValue <> m_KeypadMinValue Then
                            If KeypadPopUp.Value < m_KeypadMinValue Or KeypadPopUp.Value > m_KeypadMaxValue Then
                                System.Windows.Forms.MessageBox.Show("Value must be >" & m_KeypadMinValue & " and <" & m_KeypadMaxValue)
                                Exit Sub
                            End If
                        End If
                    Catch ex As Exception
                        System.Windows.Forms.MessageBox.Show("Failed to validate value. " & ex.Message)
                        Exit Sub
                    End Try
                    Try
                        '* 29-JAN-13 - reduced code and checked for divide by 0
                        If KeypadScaleFactor = 1 Or KeypadScaleFactor = 0 Then
                            m_CommComponent.Write(m_PLCAddressKeypad, KeypadPopUp.Value)
                        Else
                            m_CommComponent.Write(m_PLCAddressKeypad, CDbl(KeypadPopUp.Value) / m_KeypadScaleFactor)
                        End If
                    Catch ex As Exception
                        System.Windows.Forms.MessageBox.Show("Failed to write value - " & ex.Message)
                    End Try
                End If
                KeypadPopUp.Visible = False
            End If
        End If
    End Sub

    '***********************************************************
    '* If labeled is clicked, pop up a keypad for data entry
    '***********************************************************
    Protected Overrides Sub OnClick(e As System.EventArgs)
        MyBase.OnClick(e)


        If m_PLCAddressKeypad IsNot Nothing AndAlso (String.Compare(m_PLCAddressKeypad, "") <> 0) And Enabled Then
            If KeypadPopUp Is Nothing Then
                If m_KeypadAlphaNumeric Then
                    KeypadPopUp = New MfgControl.AdvancedHMI.Controls.AlphaKeyboard(m_KeypadWidth)

                Else
                    KeypadPopUp = New MfgControl.AdvancedHMI.Controls.Keypad(m_KeypadWidth)
                End If
                KeypadPopUp.StartPosition = Windows.Forms.FormStartPosition.CenterScreen
                KeypadPopUp.TopMost = True
            End If

            '***************************
            '*Set the font and forecolor
            '****************************
            'KeypadPopUp.Font = New Font("Arial", 16, FontStyle.Bold, GraphicsUnit.Point)
            If m_KeypadFont IsNot Nothing Then KeypadPopUp.Font = m_KeypadFont
            'If m_KeypadForeColor IsNot Nothing Then
            KeypadPopUp.ForeColor = m_KeypadForeColor
            'End If


            KeypadPopUp.Text = m_KeypadText
            If m_KeypadShowCurrentValue Then
                Try
                    KeypadPopUp.Value = m_CommComponent.Read(m_PLCAddressKeypad, 1)(0)
                Catch ex As Exception
                    MsgBox("Failed to read current value of " & m_PLCAddressKeypad)
                End Try
            Else
                KeypadPopUp.Value = ""
            End If
            KeypadPopUp.Visible = True
        End If
    End Sub
#End Region
End Class
