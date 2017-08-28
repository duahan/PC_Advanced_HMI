Public Class Utilities
    Public Shared Sub StopComsOnHidden(ByVal components As System.ComponentModel.IContainer, ByVal f As System.Windows.Forms.Form)
        '* V3.97d - moved this to a sharedsub to reduce code in the form
        If components IsNot Nothing Then
            Dim drv As MfgControl.AdvancedHMI.Drivers.IComComponent
            '*****************************
            '* Search for comm components
            '*****************************
            For i As Integer = 0 To components.Components.Count - 1
                If components.Components(i).GetType.GetInterface("IComComponent") IsNot Nothing Then
                    drv = DirectCast(components.Components.Item(i), MfgControl.AdvancedHMI.Drivers.IComComponent)
                    '* Stop/Start polling based on form visibility
                    drv.DisableSubscriptions = Not f.Visible
                End If
            Next
        End If
    End Sub
End Class
