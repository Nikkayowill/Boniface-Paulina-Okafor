using Okafor_.NET.Models;

namespace Okafor_.NET.ViewModels;

public class PublicHomeIndexViewModel
{
    public List<Department> FeaturedDepartments { get; set; } = [];
    public List<Doctor> FeaturedDoctors { get; set; } = [];
    public List<Post> LatestPosts { get; set; } = [];
    public List<Post> FeaturedPosts { get; set; } = [];
    public string SearchScope { get; set; } = "Entire Site";
}

public class PublicSearchResultsViewModel
{
    public string Query { get; set; } = string.Empty;
    public string Scope { get; set; } = "Entire Site";
    public List<Doctor> Doctors { get; set; } = [];
    public List<Department> Services { get; set; } = [];
    public List<Post> News { get; set; } = [];
    public List<PatientInformationTopicViewModel> PatientInformation { get; set; } = [];

    public bool HasAnyResults =>
        Doctors.Count > 0 || Services.Count > 0 || News.Count > 0 || PatientInformation.Count > 0;
}

public class PatientInformationTopicViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string LinkText { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = "#";
}
