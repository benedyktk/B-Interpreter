using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

[Export(typeof(IFileExtensionToContentTypeDefinition))]
[ContentType("desolo")]
[FileExtension(".dsl")]
internal class DesoloFileExtension { }

[Export(typeof(ContentTypeDefinition))]
[Name("desolo")]
[BaseDefinition("code")]
internal static class DesoloContentType { }
