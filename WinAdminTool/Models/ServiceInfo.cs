namespace WinAdminTool.Models;

public class ServiceInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StartType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StartName { get; set; } = string.Empty;
    public string PathName { get; set; } = string.Empty;

    // Technischer Status des Windows-Dienstes.
    // Wird für die Steuerung der Buttons verwendet.
    public string ServiceState { get; set; } = string.Empty;
}