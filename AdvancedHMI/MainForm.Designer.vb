<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    '   <System.Diagnostics.DebuggerNonUserCode()> _
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
    ' <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.DigitalPanelMeter1 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.ModbusTCPCom1 = New AdvancedHMIDrivers.ModbusTCPCom(Me.components)
        Me.Annunciator1 = New AdvancedHMIControls.Annunciator()
        Me.DigitalPanelMeter2 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.EthernetIPforCLXCom1 = New AdvancedHMIDrivers.EthernetIPforCLXCom(Me.components)
        Me.DigitalPanelMeter3 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.DigitalPanelMeter4 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.DigitalPanelMeter5 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.DigitalPanelMeter6 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.DigitalPanelMeter7 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.DigitalPanelMeter8 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.DigitalPanelMeter9 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.DigitalPanelMeter10 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.DigitalPanelMeter11 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.Annunciator2 = New AdvancedHMIControls.Annunciator()
        Me.EthernetIPforSLCMicroCom1 = New AdvancedHMIDrivers.EthernetIPforSLCMicroCom(Me.components)
        Me.DigitalPanelMeter12 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.EthernetIPforMicro800Com1 = New AdvancedHMIDrivers.EthernetIPforMicro800Com(Me.components)
        Me.DF1Com1 = New AdvancedHMIDrivers.DF1Com(Me.components)
        Me.FormChangeButton1 = New MfgControl.AdvancedHMI.FormChangeButton()
        Me.SuspendLayout()
        '
        'DigitalPanelMeter1
        '
        Me.DigitalPanelMeter1.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter1.CommComponent = Me.ModbusTCPCom1
        Me.DigitalPanelMeter1.DecimalPosition = 1
        Me.DigitalPanelMeter1.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter1.KeypadFontColor = System.Drawing.Color.Maroon
        Me.DigitalPanelMeter1.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter1.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter1.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter1.KeypadText = "Enter test"
        Me.DigitalPanelMeter1.KeypadWidth = 300
        Me.DigitalPanelMeter1.Location = New System.Drawing.Point(12, 308)
        Me.DigitalPanelMeter1.Name = "DigitalPanelMeter1"
        Me.DigitalPanelMeter1.NumberOfDigits = 6
        Me.DigitalPanelMeter1.PLCAddressKeypad = ""
        Me.DigitalPanelMeter1.PLCAddressValue = "40004"
        Me.DigitalPanelMeter1.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter1.Size = New System.Drawing.Size(125, 47)
        Me.DigitalPanelMeter1.TabIndex = 39
        Me.DigitalPanelMeter1.Text = "DigitalPanelMeter1"
        Me.DigitalPanelMeter1.Value = 0.0R
        Me.DigitalPanelMeter1.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter1.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'ModbusTCPCom1
        '
        Me.ModbusTCPCom1.DisableSubscriptions = False
        Me.ModbusTCPCom1.IPAddress = "10.2.58.124"
        Me.ModbusTCPCom1.MaxReadGroupSize = 20
        Me.ModbusTCPCom1.SwapBytes = True
        Me.ModbusTCPCom1.SwapWords = False
        Me.ModbusTCPCom1.SynchronizingObject = Me
        Me.ModbusTCPCom1.TcpipPort = CType(502US, UShort)
        Me.ModbusTCPCom1.TimeOut = 3000
        Me.ModbusTCPCom1.UnitId = CType(1, Byte)
        '
        'Annunciator1
        '
        Me.Annunciator1.CommComponent = Me.ModbusTCPCom1
        Me.Annunciator1.Location = New System.Drawing.Point(15, 256)
        Me.Annunciator1.Name = "Annunciator1"
        Me.Annunciator1.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.Toggle
        Me.Annunciator1.PLCAddressClick = "41025.0"
        Me.Annunciator1.PLCAddressText = "41025.0"
        Me.Annunciator1.PLCAddressValue = "41025.0"
        Me.Annunciator1.Size = New System.Drawing.Size(78, 46)
        Me.Annunciator1.TabIndex = 40
        Me.Annunciator1.Text = "Bit 0"
        Me.Annunciator1.Value = False
        '
        'DigitalPanelMeter2
        '
        Me.DigitalPanelMeter2.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter2.CommComponent = Me.ModbusTCPCom1
        Me.DigitalPanelMeter2.DecimalPosition = 0
        Me.DigitalPanelMeter2.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter2.KeypadFontColor = System.Drawing.Color.WhiteSmoke
        Me.DigitalPanelMeter2.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter2.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter2.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter2.KeypadText = Nothing
        Me.DigitalPanelMeter2.KeypadWidth = 300
        Me.DigitalPanelMeter2.Location = New System.Drawing.Point(15, 361)
        Me.DigitalPanelMeter2.Name = "DigitalPanelMeter2"
        Me.DigitalPanelMeter2.NumberOfDigits = 5
        Me.DigitalPanelMeter2.PLCAddressKeypad = ""
        Me.DigitalPanelMeter2.PLCAddressValue = "41025"
        Me.DigitalPanelMeter2.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter2.Size = New System.Drawing.Size(106, 46)
        Me.DigitalPanelMeter2.TabIndex = 41
        Me.DigitalPanelMeter2.Text = "Command Output"
        Me.DigitalPanelMeter2.Value = 0.0R
        Me.DigitalPanelMeter2.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter2.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'EthernetIPforCLXCom1
        '
        Me.EthernetIPforCLXCom1.CIPConnectionSize = 508
        Me.EthernetIPforCLXCom1.DisableMultiServiceRequest = False
        Me.EthernetIPforCLXCom1.DisableSubscriptions = False
        Me.EthernetIPforCLXCom1.IPAddress = "10.2.58.125"
        Me.EthernetIPforCLXCom1.PollRateOverride = 500
        Me.EthernetIPforCLXCom1.Port = 44818
        Me.EthernetIPforCLXCom1.ProcessorSlot = 0
        '
        'DigitalPanelMeter3
        '
        Me.DigitalPanelMeter3.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter3.CommComponent = Me.EthernetIPforCLXCom1
        Me.DigitalPanelMeter3.DecimalPosition = 0
        Me.DigitalPanelMeter3.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter3.ImeMode = System.Windows.Forms.ImeMode.[On]
        Me.DigitalPanelMeter3.KeypadFontColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter3.KeypadMaxValue = 32767.0R
        Me.DigitalPanelMeter3.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter3.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter3.KeypadText = "Enter Command"
        Me.DigitalPanelMeter3.KeypadWidth = 300
        Me.DigitalPanelMeter3.Location = New System.Drawing.Point(602, 12)
        Me.DigitalPanelMeter3.Name = "DigitalPanelMeter3"
        Me.DigitalPanelMeter3.NumberOfDigits = 5
        Me.DigitalPanelMeter3.PLCAddressKeypad = "Command"
        Me.DigitalPanelMeter3.PLCAddressValue = "Command"
        Me.DigitalPanelMeter3.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter3.Size = New System.Drawing.Size(170, 74)
        Me.DigitalPanelMeter3.TabIndex = 43
        Me.DigitalPanelMeter3.Text = "Command Value"
        Me.DigitalPanelMeter3.Value = 0.0R
        Me.DigitalPanelMeter3.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter3.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'DigitalPanelMeter4
        '
        Me.DigitalPanelMeter4.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter4.CommComponent = Me.EthernetIPforCLXCom1
        Me.DigitalPanelMeter4.DecimalPosition = 0
        Me.DigitalPanelMeter4.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter4.ImeMode = System.Windows.Forms.ImeMode.[On]
        Me.DigitalPanelMeter4.KeypadFontColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter4.KeypadMaxValue = 32767.0R
        Me.DigitalPanelMeter4.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter4.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter4.KeypadText = "Enter Parameter"
        Me.DigitalPanelMeter4.KeypadWidth = 300
        Me.DigitalPanelMeter4.Location = New System.Drawing.Point(602, 107)
        Me.DigitalPanelMeter4.Name = "DigitalPanelMeter4"
        Me.DigitalPanelMeter4.NumberOfDigits = 5
        Me.DigitalPanelMeter4.PLCAddressKeypad = "Scale_Output[1]"
        Me.DigitalPanelMeter4.PLCAddressValue = "Scale_Output[1]"
        Me.DigitalPanelMeter4.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter4.Size = New System.Drawing.Size(170, 74)
        Me.DigitalPanelMeter4.TabIndex = 44
        Me.DigitalPanelMeter4.Text = "Parameter Value"
        Me.DigitalPanelMeter4.Value = 0.0R
        Me.DigitalPanelMeter4.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter4.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'DigitalPanelMeter5
        '
        Me.DigitalPanelMeter5.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter5.CommComponent = Me.EthernetIPforCLXCom1
        Me.DigitalPanelMeter5.DecimalPosition = 0
        Me.DigitalPanelMeter5.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter5.ImeMode = System.Windows.Forms.ImeMode.[On]
        Me.DigitalPanelMeter5.KeypadFontColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter5.KeypadMaxValue = 32767.0R
        Me.DigitalPanelMeter5.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter5.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter5.KeypadText = "Enter Setpoint"
        Me.DigitalPanelMeter5.KeypadWidth = 300
        Me.DigitalPanelMeter5.Location = New System.Drawing.Point(602, 296)
        Me.DigitalPanelMeter5.Name = "DigitalPanelMeter5"
        Me.DigitalPanelMeter5.NumberOfDigits = 5
        Me.DigitalPanelMeter5.PLCAddressKeypad = "Setpoint_Value"
        Me.DigitalPanelMeter5.PLCAddressValue = "Setpoint_Value"
        Me.DigitalPanelMeter5.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter5.Size = New System.Drawing.Size(170, 74)
        Me.DigitalPanelMeter5.TabIndex = 45
        Me.DigitalPanelMeter5.Text = "Setpoint Value"
        Me.DigitalPanelMeter5.Value = 0.0R
        Me.DigitalPanelMeter5.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter5.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'DigitalPanelMeter6
        '
        Me.DigitalPanelMeter6.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter6.CommComponent = Me.EthernetIPforCLXCom1
        Me.DigitalPanelMeter6.DecimalPosition = 0
        Me.DigitalPanelMeter6.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter6.ImeMode = System.Windows.Forms.ImeMode.[On]
        Me.DigitalPanelMeter6.KeypadFontColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter6.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter6.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter6.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter6.KeypadText = ""
        Me.DigitalPanelMeter6.KeypadWidth = 300
        Me.DigitalPanelMeter6.Location = New System.Drawing.Point(379, 12)
        Me.DigitalPanelMeter6.Name = "DigitalPanelMeter6"
        Me.DigitalPanelMeter6.NumberOfDigits = 5
        Me.DigitalPanelMeter6.PLCAddressKeypad = ""
        Me.DigitalPanelMeter6.PLCAddressValue = "Scale_Input[0]"
        Me.DigitalPanelMeter6.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter6.Size = New System.Drawing.Size(170, 74)
        Me.DigitalPanelMeter6.TabIndex = 46
        Me.DigitalPanelMeter6.Text = "Returned Command"
        Me.DigitalPanelMeter6.Value = 0.0R
        Me.DigitalPanelMeter6.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter6.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'DigitalPanelMeter7
        '
        Me.DigitalPanelMeter7.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter7.CommComponent = Me.EthernetIPforCLXCom1
        Me.DigitalPanelMeter7.DecimalPosition = 0
        Me.DigitalPanelMeter7.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter7.ImeMode = System.Windows.Forms.ImeMode.[On]
        Me.DigitalPanelMeter7.KeypadFontColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter7.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter7.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter7.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter7.KeypadText = ""
        Me.DigitalPanelMeter7.KeypadWidth = 300
        Me.DigitalPanelMeter7.Location = New System.Drawing.Point(379, 107)
        Me.DigitalPanelMeter7.Name = "DigitalPanelMeter7"
        Me.DigitalPanelMeter7.NumberOfDigits = 5
        Me.DigitalPanelMeter7.PLCAddressKeypad = ""
        Me.DigitalPanelMeter7.PLCAddressValue = "Scale_Input[1]"
        Me.DigitalPanelMeter7.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter7.Size = New System.Drawing.Size(170, 74)
        Me.DigitalPanelMeter7.TabIndex = 47
        Me.DigitalPanelMeter7.Text = "Status"
        Me.DigitalPanelMeter7.Value = 0.0R
        Me.DigitalPanelMeter7.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter7.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'DigitalPanelMeter8
        '
        Me.DigitalPanelMeter8.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter8.CommComponent = Me.EthernetIPforCLXCom1
        Me.DigitalPanelMeter8.DecimalPosition = 0
        Me.DigitalPanelMeter8.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter8.ImeMode = System.Windows.Forms.ImeMode.[On]
        Me.DigitalPanelMeter8.KeypadFontColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter8.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter8.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter8.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter8.KeypadText = ""
        Me.DigitalPanelMeter8.KeypadWidth = 300
        Me.DigitalPanelMeter8.Location = New System.Drawing.Point(379, 201)
        Me.DigitalPanelMeter8.Name = "DigitalPanelMeter8"
        Me.DigitalPanelMeter8.NumberOfDigits = 5
        Me.DigitalPanelMeter8.PLCAddressKeypad = ""
        Me.DigitalPanelMeter8.PLCAddressValue = "Scale_Input[2]"
        Me.DigitalPanelMeter8.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter8.Size = New System.Drawing.Size(170, 74)
        Me.DigitalPanelMeter8.TabIndex = 48
        Me.DigitalPanelMeter8.Text = "MSW"
        Me.DigitalPanelMeter8.Value = 0.0R
        Me.DigitalPanelMeter8.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter8.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'DigitalPanelMeter9
        '
        Me.DigitalPanelMeter9.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter9.CommComponent = Me.EthernetIPforCLXCom1
        Me.DigitalPanelMeter9.DecimalPosition = 0
        Me.DigitalPanelMeter9.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter9.ImeMode = System.Windows.Forms.ImeMode.[On]
        Me.DigitalPanelMeter9.KeypadFontColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter9.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter9.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter9.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter9.KeypadText = ""
        Me.DigitalPanelMeter9.KeypadWidth = 300
        Me.DigitalPanelMeter9.Location = New System.Drawing.Point(379, 296)
        Me.DigitalPanelMeter9.Name = "DigitalPanelMeter9"
        Me.DigitalPanelMeter9.NumberOfDigits = 5
        Me.DigitalPanelMeter9.PLCAddressKeypad = ""
        Me.DigitalPanelMeter9.PLCAddressValue = "Scale_Input[3]"
        Me.DigitalPanelMeter9.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter9.Size = New System.Drawing.Size(170, 74)
        Me.DigitalPanelMeter9.TabIndex = 49
        Me.DigitalPanelMeter9.Text = "LSW"
        Me.DigitalPanelMeter9.Value = 0.0R
        Me.DigitalPanelMeter9.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter9.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'DigitalPanelMeter10
        '
        Me.DigitalPanelMeter10.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter10.CommComponent = Me.EthernetIPforCLXCom1
        Me.DigitalPanelMeter10.DecimalPosition = 0
        Me.DigitalPanelMeter10.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter10.ImeMode = System.Windows.Forms.ImeMode.[On]
        Me.DigitalPanelMeter10.KeypadFontColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter10.KeypadMaxValue = 32767.0R
        Me.DigitalPanelMeter10.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter10.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter10.KeypadText = "Enter Setpoint"
        Me.DigitalPanelMeter10.KeypadWidth = 300
        Me.DigitalPanelMeter10.Location = New System.Drawing.Point(602, 201)
        Me.DigitalPanelMeter10.Name = "DigitalPanelMeter10"
        Me.DigitalPanelMeter10.NumberOfDigits = 5
        Me.DigitalPanelMeter10.PLCAddressKeypad = ""
        Me.DigitalPanelMeter10.PLCAddressValue = "Weight_Value"
        Me.DigitalPanelMeter10.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter10.Size = New System.Drawing.Size(170, 74)
        Me.DigitalPanelMeter10.TabIndex = 50
        Me.DigitalPanelMeter10.Text = "Weight Value"
        Me.DigitalPanelMeter10.Value = 0.0R
        Me.DigitalPanelMeter10.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter10.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'DigitalPanelMeter11
        '
        Me.DigitalPanelMeter11.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter11.CommComponent = Me.EthernetIPforCLXCom1
        Me.DigitalPanelMeter11.DecimalPosition = 0
        Me.DigitalPanelMeter11.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter11.ImeMode = System.Windows.Forms.ImeMode.[On]
        Me.DigitalPanelMeter11.KeypadFontColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter11.KeypadMaxValue = 999999.0R
        Me.DigitalPanelMeter11.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter11.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter11.KeypadText = "Enter Setpoint"
        Me.DigitalPanelMeter11.KeypadWidth = 300
        Me.DigitalPanelMeter11.Location = New System.Drawing.Point(602, 419)
        Me.DigitalPanelMeter11.Name = "DigitalPanelMeter11"
        Me.DigitalPanelMeter11.NumberOfDigits = 6
        Me.DigitalPanelMeter11.PLCAddressKeypad = ""
        Me.DigitalPanelMeter11.PLCAddressValue = "Average_Time"
        Me.DigitalPanelMeter11.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter11.Size = New System.Drawing.Size(170, 64)
        Me.DigitalPanelMeter11.TabIndex = 51
        Me.DigitalPanelMeter11.Text = "Time Value"
        Me.DigitalPanelMeter11.Value = 0.0R
        Me.DigitalPanelMeter11.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter11.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'Annunciator2
        '
        Me.Annunciator2.CommComponent = Me.EthernetIPforCLXCom1
        Me.Annunciator2.ForeColor = System.Drawing.Color.Black
        Me.Annunciator2.Location = New System.Drawing.Point(15, 190)
        Me.Annunciator2.Name = "Annunciator2"
        Me.Annunciator2.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.Annunciator2.PLCAddressValue = "Scale_Input[3]"
        Me.Annunciator2.Size = New System.Drawing.Size(87, 46)
        Me.Annunciator2.TabIndex = 52
        Me.Annunciator2.Text = "Start Test"
        Me.Annunciator2.Value = False
        '
        'EthernetIPforSLCMicroCom1
        '
        Me.EthernetIPforSLCMicroCom1.CIPConnectionSize = 508
        Me.EthernetIPforSLCMicroCom1.DisableSubscriptions = False
        Me.EthernetIPforSLCMicroCom1.IPAddress = "10.2.58.124"
        Me.EthernetIPforSLCMicroCom1.PollRateOverride = 100
        Me.EthernetIPforSLCMicroCom1.Port = 44818
        Me.EthernetIPforSLCMicroCom1.SynchronizingObject = Me
        '
        'DigitalPanelMeter12
        '
        Me.DigitalPanelMeter12.BackColor = System.Drawing.Color.Transparent
        Me.DigitalPanelMeter12.CommComponent = Me.EthernetIPforMicro800Com1
        Me.DigitalPanelMeter12.DecimalPosition = 0
        Me.DigitalPanelMeter12.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter12.KeypadFontColor = System.Drawing.Color.WhiteSmoke
        Me.DigitalPanelMeter12.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter12.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter12.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter12.KeypadText = Nothing
        Me.DigitalPanelMeter12.KeypadWidth = 300
        Me.DigitalPanelMeter12.Location = New System.Drawing.Point(17, 26)
        Me.DigitalPanelMeter12.Name = "DigitalPanelMeter12"
        Me.DigitalPanelMeter12.NumberOfDigits = 5
        Me.DigitalPanelMeter12.PLCAddressKeypad = ""
        Me.DigitalPanelMeter12.PLCAddressValue = "EIP_920i_1.Command"
        Me.DigitalPanelMeter12.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter12.Size = New System.Drawing.Size(233, 101)
        Me.DigitalPanelMeter12.TabIndex = 54
        Me.DigitalPanelMeter12.Text = "DigitalPanelMeter12"
        Me.DigitalPanelMeter12.Value = 0.0R
        Me.DigitalPanelMeter12.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter12.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'EthernetIPforMicro800Com1
        '
        Me.EthernetIPforMicro800Com1.CIPConnectionSize = 508
        Me.EthernetIPforMicro800Com1.DisableMultiServiceRequest = False
        Me.EthernetIPforMicro800Com1.DisableSubscriptions = False
        Me.EthernetIPforMicro800Com1.IPAddress = "10.2.58.124"
        Me.EthernetIPforMicro800Com1.PollRateOverride = 500
        Me.EthernetIPforMicro800Com1.Port = 44818
        Me.EthernetIPforMicro800Com1.ProcessorSlot = 0
        '
        'DF1Com1
        '
        Me.DF1Com1.BaudRate = "AUTO"
        Me.DF1Com1.CheckSumType = MfgControl.AdvancedHMI.Drivers.DF1Transport.ChecksumOptions.Crc
        Me.DF1Com1.ComPort = "COM1"
        Me.DF1Com1.DisableSubscriptions = False
        Me.DF1Com1.MyNode = 0
        Me.DF1Com1.Parity = System.IO.Ports.Parity.None
        Me.DF1Com1.PollRateOverride = 100
        Me.DF1Com1.SynchronizingObject = Me
        Me.DF1Com1.TargetNode = 0
        '
        'FormChangeButton1
        '
        Me.FormChangeButton1.BackColor = System.Drawing.SystemColors.ButtonFace
        Me.FormChangeButton1.CommComponent = Nothing
        Me.FormChangeButton1.ForeColor = System.Drawing.Color.Black
        Me.FormChangeButton1.FormToOpen = GetType(MfgControl.AdvancedHMI.Page2)
        Me.FormChangeButton1.KeypadWidth = 300
        Me.FormChangeButton1.Location = New System.Drawing.Point(32, 419)
        Me.FormChangeButton1.Name = "FormChangeButton1"
        Me.FormChangeButton1.Passcode = Nothing
        Me.FormChangeButton1.PLCAddressVisible = ""
        Me.FormChangeButton1.Size = New System.Drawing.Size(130, 65)
        Me.FormChangeButton1.TabIndex = 55
        Me.FormChangeButton1.Text = "FormChangeButton1"
        Me.FormChangeButton1.UseVisualStyleBackColor = False
        '
        'MainForm
        '
        Me.AutoScroll = True
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(784, 562)
        Me.Controls.Add(Me.FormChangeButton1)
        Me.Controls.Add(Me.DigitalPanelMeter12)
        Me.Controls.Add(Me.Annunciator2)
        Me.Controls.Add(Me.DigitalPanelMeter11)
        Me.Controls.Add(Me.DigitalPanelMeter10)
        Me.Controls.Add(Me.DigitalPanelMeter9)
        Me.Controls.Add(Me.DigitalPanelMeter8)
        Me.Controls.Add(Me.DigitalPanelMeter7)
        Me.Controls.Add(Me.DigitalPanelMeter6)
        Me.Controls.Add(Me.DigitalPanelMeter5)
        Me.Controls.Add(Me.DigitalPanelMeter4)
        Me.Controls.Add(Me.DigitalPanelMeter3)
        Me.Controls.Add(Me.DigitalPanelMeter2)
        Me.Controls.Add(Me.Annunciator1)
        Me.Controls.Add(Me.DigitalPanelMeter1)
        Me.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ForeColor = System.Drawing.Color.White
        Me.KeyPreview = True
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "AdvancedHMI v3.99c"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents DigitalPanelMeter1 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents Annunciator1 As AdvancedHMIControls.Annunciator
    Friend WithEvents DigitalPanelMeter2 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents DigitalPanelMeter3 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents EthernetIPforCLXCom1 As AdvancedHMIDrivers.EthernetIPforCLXCom
    Friend WithEvents ModbusTCPCom1 As AdvancedHMIDrivers.ModbusTCPCom
    Friend WithEvents DigitalPanelMeter7 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents DigitalPanelMeter6 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents DigitalPanelMeter5 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents DigitalPanelMeter4 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents DigitalPanelMeter9 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents DigitalPanelMeter8 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents DigitalPanelMeter10 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents DigitalPanelMeter11 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents Annunciator2 As AdvancedHMIControls.Annunciator
    Friend WithEvents DigitalPanelMeter12 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents EthernetIPforSLCMicroCom1 As AdvancedHMIDrivers.EthernetIPforSLCMicroCom
    Friend WithEvents DF1Com1 As AdvancedHMIDrivers.DF1Com
    Friend WithEvents FormChangeButton1 As MfgControl.AdvancedHMI.FormChangeButton
    Friend WithEvents EthernetIPforMicro800Com1 As AdvancedHMIDrivers.EthernetIPforMicro800Com
End Class
