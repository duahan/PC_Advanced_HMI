Public Class MainForm
    '*******************************************************************************
    '* Stop polling when the form is not visible in order to reduce communications
    '* Copy this section of code to every new form created
    '*******************************************************************************
    Private NotFirstShow As Boolean
    Private Sub Form_VisibleChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.VisibleChanged
        '* Do not start comms on first show in case it was set to disable in design mode
        If NotFirstShow Then
            AdvancedHMIDrivers.Utilities.StopComsOnHidden(components, Me)
        Else
            NotFirstShow = True
        End If
    End Sub


    Private Sub DigitalPanelMeter12_Click(sender As Object, e As EventArgs) Handles DigitalPanelMeter12.Click

    End Sub

    Private Sub EthernetIPforMicro800Com1_DataReceived(sender As Object, e As Drivers.Common.PlcComEventArgs) Handles EthernetIPforMicro800Com1.DataReceived

    End Sub
End Class
