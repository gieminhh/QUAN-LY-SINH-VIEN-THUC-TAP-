using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using InternshipManagement.Web.ViewModels;
namespace InternshipManagement.Web.Services;

public static class XlsxUsersReader
{
    static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    static XDocument Read(ZipArchiveEntry e) { using var stream = e.Open(); using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, MaxCharactersInDocument = 8_000_000 }); return XDocument.Load(reader); }
    public static List<ImportUserRow> Read(Stream input)
    {
        using var zip = new ZipArchive(input, ZipArchiveMode.Read);
        if (zip.Entries.Count > 1000 || zip.Entries.Sum(e => e.Length) > 20_000_000) throw new InvalidDataException("File Excel quá lớn sau giải nén.");
        var strings = zip.GetEntry("xl/sharedStrings.xml") is { } shared ? Read(shared).Root!.Elements(S + "si").Select(x => string.Concat(x.Descendants(S + "t").Select(t => t.Value))).ToList() : new List<string>();
        var book = Read(zip.GetEntry("xl/workbook.xml") ?? throw new InvalidDataException("Thiếu workbook."));
        var sheet = book.Descendants(S + "sheet").FirstOrDefault() ?? throw new InvalidDataException("Không có worksheet.");
        var rid = (string?)sheet.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
        var rel = Read(zip.GetEntry("xl/_rels/workbook.xml.rels")!);
        var target = (string?)rel.Root!.Elements().First(x => (string?)x.Attribute("Id") == rid).Attribute("Target") ?? "";
        var path = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
        if (path.Contains("..")) throw new InvalidDataException("Đường dẫn worksheet không hợp lệ.");
        var rows = Read(zip.GetEntry(path) ?? throw new InvalidDataException("Không tìm thấy worksheet.")).Descendants(S + "row").ToList();
        if (rows.Count > 501 || rows.Count < 2) throw new InvalidDataException("Nhập từ 1 đến 500 dòng dữ liệu.");
        Dictionary<string, string> Cells(XElement row) => row.Elements(S + "c").ToDictionary(c => new string(((string?)c.Attribute("r") ?? "").TakeWhile(char.IsLetter).ToArray()), c => c.Element(S + "f") != null ? throw new InvalidDataException("Không dùng công thức trong file nhập.") : (string?)c.Attribute("t") == "s" ? strings[int.Parse(c.Element(S + "v")!.Value)] : c.Element(S + "is") != null ? string.Concat(c.Descendants(S + "t").Select(t => t.Value)) : c.Element(S + "v")?.Value ?? "");
        var header = Cells(rows[0]); var names = new[] { "UserName", "Email", "FullName", "Role", "InitialPassword" };
        foreach (var (name, index) in names.Select((name, index) => (name, index))) if (!header.TryGetValue(((char)('A' + index)).ToString(), out var value) || value.Trim() != name) throw new InvalidDataException("Tiêu đề phải là UserName, Email, FullName, Role, InitialPassword theo thứ tự A–E.");
        return rows.Skip(1).Select((row, i) => { var c = Cells(row); string Get(string col) => c.GetValueOrDefault(col, "").Trim(); return new ImportUserRow((int?)row.Attribute("r") ?? i + 2, Get("A"), Get("B"), Get("C"), Get("D"), Get("E")); }).Where(x => x.UserName != "" || x.Email != "").ToList();
    }
}
