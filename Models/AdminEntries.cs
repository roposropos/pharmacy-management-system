using System;

namespace Apteka.Models;

public class AuditLogEntry
{
	public int Id { get; set; }
	public DateTime DataOperacji { get; set; }
	public string TypOperacji { get; set; } = string.Empty;
	public string Encja { get; set; } = string.Empty;
	public string? KluczRekordu { get; set; }
	public string? Opis { get; set; }
	public string? Login { get; set; }
	public string? Uzytkownik { get; set; }
}

public class BackupFileEntry
{
	public string FileName { get; set; } = string.Empty;
	public string FullPath { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public long SizeBytes { get; set; }
	public string SizeLabel => SizeBytes < 1024 * 1024
		? $"{SizeBytes / 1024.0:0.0} KB"
		: $"{SizeBytes / 1024.0 / 1024.0:0.0} MB";
}
