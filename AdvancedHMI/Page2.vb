Public Class Page2
    '*******************************************************************************
    '* Stop polling when the form is not visible in order to reduce communications
    '*******************************************************************************
    Private Sub Form_VisibleChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.VisibleChanged
        AdvancedHMIDrivers.Utilities.StopComsOnHidden(components, Me)
    End Sub

    Private Sub ReturnToMainButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        MainForm.Show()
        ' Me.Hide()
        Me.Visible = False
    End Sub
End Class