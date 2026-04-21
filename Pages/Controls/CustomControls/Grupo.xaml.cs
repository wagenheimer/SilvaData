namespace SilvaData.Controls
{
    public partial class Grupo : ContentView
    {
        public Grupo()
        {
            try
            {
                InitializeComponent();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRASH_INICIALIZACAO] Grupo: {ex.Message}");
                var inner = ex.InnerException;
                while (inner != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[CRASH_INNER] {inner.Message}");
                    System.Diagnostics.Debug.WriteLine($"[CRASH_TRACE] {inner.StackTrace}");
                    inner = inner.InnerException;
                }
                throw;
            }
        }
    }
}
