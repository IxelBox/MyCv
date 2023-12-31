using System.ComponentModel.DataAnnotations;

namespace MyCv.Model;

public record struct SideStructure([Required]MyPrintCv MyPrintCv, MyWebCv MyWebCv, OtherSide[] Sides, [Required]PersonalInformation PersonalInformation);

public record struct PersonalInformation(
    [Required] string Name,
    CvPersonalImage Image,
    string Birthplace,
    string Birthday,
    string EMail,
    string Mobil,
    string Homepage);
public record struct MyWebCv(CvEntry[] Entries, CvSkill[] KeyFacts);

public record struct MyPrintCv(CvSection[] Sections);

public record struct CvSection([Required]string Title, CvSkill[]? Skills = null, CvEntry[]? Entries = null, CvSection[]? SubSections = null);
public record struct CvEntry([Required]string TimePeriod, [Required]string Text, string? ShortTitle = null, string?  Additional = null, bool NewLineToBr = false);
public record struct CvSkill([Required]string Text);

public record struct CvPersonalImage([Required]string Path, [Required]string AlternativeText, int Width, int PrintWith);

public record struct OtherSide([Required]string Title, bool InMainNav, OtherSideSection[] Sections);
public record struct OtherSideSection([Required]string Title, string Text, OtherSideSection[]? SubEntry = null);

