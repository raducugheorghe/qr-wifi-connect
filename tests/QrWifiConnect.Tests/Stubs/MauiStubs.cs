// Minimal stubs for MAUI types used in ViewModel declarations.
// These stubs allow ViewModels to compile for net9.0 (test target) without
// requiring the maui-maccatalyst workload. They are runtime no-ops used only
// for type identity — Shell resolves them at runtime in the actual MAUI app.

// ReSharper disable All
namespace Microsoft.Maui.Controls;

internal interface IQueryAttributable
{
    void ApplyQueryAttributes(IDictionary<string, object> query);
}
