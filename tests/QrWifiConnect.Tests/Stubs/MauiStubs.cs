// Minimal stubs for MAUI types used in ViewModel declarations.
// These stubs allow ViewModels to compile for net9.0 (test target) without
// requiring the maui-maccatalyst workload. They are runtime no-ops used only
// for type identity — Shell resolves them at runtime in the actual MAUI app.

// ReSharper disable All
#pragma warning disable CS9113 // suppress "parameter unused" for positional records
namespace Microsoft.Maui.Controls;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class QueryPropertyAttribute(string name, string queryId) : Attribute;
