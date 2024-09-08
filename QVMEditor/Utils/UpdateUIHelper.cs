using System;
using System.Reflection;
using System.Windows.Forms;

public static class UIUpdateHelper
{
    /// <summary>
    /// Updates the property of a UI control in a thread-safe manner.
    /// </summary>
    /// <param name="control">The control to update.</param>
    /// <param name="propertyName">The name of the property to update.</param>
    /// <param name="value">The new value for the property.</param>
    public static void UpdateControlProperty(Control control, string propertyName, object value)
    {
        try
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new Action(() => ApplyPropertyUpdate(control, propertyName, value)));
            }
            else
            {
                ApplyPropertyUpdate(control, propertyName, value);
            }
        }
        catch (Exception ex)
        {
            LogException(MethodBase.GetCurrentMethod().Name, ex);
        }
    }

    private static void ApplyPropertyUpdate(Control control, string propertyName, object value)
    {
        // Get the property information based on the property name provided.
        PropertyInfo propertyInfo = control.GetType().GetProperty(propertyName);
        if (propertyInfo != null)
        {
            propertyInfo.SetValue(control, value, null);
            control.Invalidate();
            control.Parent?.Invalidate(); // Invalidate the parent container to ensure the layout updates.
            control.Update(); // Force the immediate redraw of the control.
        }
    }

    /// <summary>
    /// Logs exceptions.
    /// </summary>
    /// <param name="methodName">Name of the method where exception occurred.</param>
    /// <param name="ex">Exception that occurred.</param>
    private static void LogException(string methodName, Exception ex)
    {
        Console.WriteLine($"Exception in {methodName}: {ex.Message}");
    }
}
