using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Okafor_.NET.Areas.Patient.Controllers;

[Area("Patient")]
[Authorize(Roles = "Patient")]
public abstract class PatientBaseController : Controller
{
}
