using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Okafor_.NET.Controllers;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Tests;

public sealed class DoctorsControllerTests
{
    [Fact]
    public async Task Create_GeneratesSlugAndPersistsDoctor()
    {
        await using var db = CreateContext();
        db.Departments.Add(new Department { Name = "Cardiology" });
        await db.SaveChangesAsync();

        var controller = new DoctorsController(db);

        var result = await controller.Create(new Doctor
        {
            FullName = "Dr. Jane Doe",
            Specialty = "Cardiology",
            DepartmentId = 1
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var savedDoctor = await db.Doctors.SingleAsync();
        Assert.Equal("dr-jane-doe", savedDoctor.Slug);
        Assert.Equal("Dr. Jane Doe", savedDoctor.FullName);
    }

    [Fact]
    public async Task Edit_ReturnsViewWhenSlugAlreadyExists()
    {
        await using var db = CreateContext();
        db.Departments.Add(new Department { Name = "General Medicine" });
        db.Doctors.AddRange(
            new Doctor
            {
                Id = 1,
                FullName = "Existing Doctor",
                Slug = "shared-slug",
                Specialty = "Internal Medicine",
                DepartmentId = 1
            },
            new Doctor
            {
                Id = 2,
                FullName = "Target Doctor",
                Specialty = "Family Medicine",
                DepartmentId = 1
            });
        await db.SaveChangesAsync();

        var controller = new DoctorsController(db);

        var result = await controller.Edit(2, new Doctor
        {
            Id = 2,
            FullName = "Target Doctor",
            Slug = "shared slug",
            Specialty = "Family Medicine",
            DepartmentId = 1
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Doctor.Slug)));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
