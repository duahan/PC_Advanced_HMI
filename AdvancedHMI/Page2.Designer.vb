<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Page2
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.DigitalPanelMeter12 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.EthernetIPforSLCMicroCom2 = New AdvancedHMIDrivers.EthernetIPforSLCMicroCom(Me.components)
        Me.FormChangeButton1 = New MfgControl.AdvancedHMI.FormChangeButton()
        Me.SuspendLayout()
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(163, 260)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(136, 36)
        Me.Button1.TabIndex = 2
        Me.Button1.Text = "Close"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'DigitalPanelMeter12
        '
        Me.DigitalPanelMeter12.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter12.CommComponent = Me.EthernetIPforSLCMicroCom2
        Me.DigitalPanelMeter12.DecimalPosition = 0
        Me.DigitalPanelMeter12.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter12.KeypadFontColor = System.Drawing.Color.WhiteSmoke
        Me.DigitalPanelMeter12.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter12.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter12.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter12.KeypadText = Nothing
        Me.DigitalPanelMeter12.KeypadWidth = 300
        Me.DigitalPanelMeter12.Location = New System.Drawing.Point(114, 116)
        Me.DigitalPanelMeter12.Name = "DigitalPanelMeter12"
        Me.DigitalPanelMeter12.NumberOfDigits = 5
        Me.DigitalPanelMeter12.PLCAddressKeypad = ""
        Me.DigitalPanelMeter12.PLCAddressValue = "N7:2"
        Me.DigitalPanelMeter12.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter12.Size = New System.Drawing.Size(233, 101)
        Me.DigitalPanelMeter12.TabIndex = 55
        Me.DigitalPanelMeter12.Text = "DigitalPanelMeter12"
        Me.DigitalPanelMeter12.Value = 0.0R
        Me.DigitalPanelMeter12.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter12.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'EthernetIPforSLCMicroCom2
        '
        Me.EthernetIPforSLCMicroCom2.CIPConnectionSize = 508
        Me.EthernetIPforSLCMicroCom2.DisableSubscriptions = False
        Me.EthernetIPforSLCMicroCom2.IPAddress = "10.2.58.124"
        Me.EthernetIPforSLCMicroCom2.PollRateOverride = 100
        Me.EthernetIPforSLCMicroCom2.Port = 44818
        Me.EthernetIPforSLCMicroCom2.SynchronizingObject = Me
        '
        'FormChangeButton1
        '
        Me.FormChangeButton1.BackColor = System.Drawing.SystemColors.ButtonFace
        Me.FormChangeButton1.CommComponent = Nothing
        Me.FormChangeButton1.ForeColor = System.Drawing.Color.Black
        Me.FormChangeButton1.FormToOpen = GetType(MfgControl.AdvancedHMI.MainForm)
        Me.FormChangeButton1.KeypadWidth = 300
        Me.FormChangeButton1.Location = New System.Drawing.Point(163, 28)
        Me.FormChangeButton1.Name = "FormChangeButton1"
        Me.FormChangeButton1.Passcode = Nothing
        Me.FormChangeButton1.PLCAddressVisible = ""
        Me.FormChangeButton1.Size = New System.Drawing.Size(130, 65)
        Me.FormChangeButton1.TabIndex = 3
        Me.FormChangeButton1.Text = "FormChangeButton1"
        Me.FormChangeButton1.UseVisualStyleBackColor = False
        '
        'Page2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ControlDarkDark
        Me.ClientSize = New System.Drawing.Size(461, 333)
        Me.Controls.Add(Me.DigitalPanelMeter12)
        Me.Controls.Add(Me.FormChangeButton1)
        Me.Controls.Add(Me.Button1)
        Me.Name = "Page2"
        Me.Text = "Page2"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents FormChangeButton1 As MfgControl.AdvancedHMI.FormChangeButton
    Friend WithEvents DigitalPanelMeter12 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents EthernetIPforSLCMicroCom2 As AdvancedHMIDrivers.EthernetIPforSLCMicroCom
End Class
