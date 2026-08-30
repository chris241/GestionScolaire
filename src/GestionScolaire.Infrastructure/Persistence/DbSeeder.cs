using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using GestionScolaire.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Infrastructure.Persistence;

/// Données de démonstration pour l'environnement de développement. Idempotent : ne s'exécute que si la base est vide.
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync()) return;

        var director = new User
        {
            Email = "directeur@ecole.mg",
            PasswordHash = PasswordHasher.Hash("Password123!"),
            FirstName = "Rina",
            LastName = "Rakoto",
            Role = UserRole.Director
        };
        context.Users.Add(director);

        var schoolLumiere = new School
        {
            Name = "Lumière",
            Address = "Antananarivo",
            Currency = "MGA",
            DefaultMaxScore = 20,
            Director = director
        };
        var schoolGenie = new School
        {
            Name = "Génie",
            Address = "Fianarantsoa",
            Currency = "MGA",
            DefaultMaxScore = 20,
            Director = director
        };
        context.Schools.AddRange(schoolLumiere, schoolGenie);
        director.LastActiveSchoolId = schoolLumiere.Id;

        var subjects = new[]
        {
            new Subject { Name = "Mathématiques", Coefficient = 4, School = schoolLumiere },
            new Subject { Name = "Français", Coefficient = 4, School = schoolLumiere },
            new Subject { Name = "Sciences", Coefficient = 3, School = schoolLumiere },
            new Subject { Name = "Histoire-Géographie", Coefficient = 2, School = schoolLumiere },
            new Subject { Name = "Anglais", Coefficient = 2, School = schoolLumiere },
        };
        context.Subjects.AddRange(subjects);

        var academicYear = new AcademicYear
        {
            Name = "2025-2026",
            StartDate = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            IsCurrent = true,
            School = schoolLumiere
        };
        context.AcademicYears.Add(academicYear);

        // Année académique isolée pour la 2ᵉ école, pour prouver le cloisonnement des données entre écoles.
        var academicYearGenie = new AcademicYear
        {
            Name = "2025-2026",
            StartDate = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            // Pas "courante" : le formulaire public de candidature (pas encore scopé par école, voir phase 2)
            // pioche la première année IsCurrent tous établissements confondus — éviter toute ambiguïté ici.
            IsCurrent = false,
            School = schoolGenie
        };
        context.AcademicYears.Add(academicYearGenie);

        var academicTerms = new[]
        {
            new AcademicTerm { Name = "Trimestre 1", Order = 1, AcademicYear = academicYear, School = schoolLumiere, StartDate = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 19, 0, 0, 0, DateTimeKind.Utc) },
            new AcademicTerm { Name = "Trimestre 2", Order = 2, AcademicYear = academicYear, School = schoolLumiere, StartDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc) },
            new AcademicTerm { Name = "Trimestre 3", Order = 3, AcademicYear = academicYear, School = schoolLumiere, StartDate = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc) },
        };
        context.AcademicTerms.AddRange(academicTerms);

        var teacherUsers = new[]
        {
            new User { Email = "prof.math@ecole.mg", PasswordHash = PasswordHasher.Hash("Password123!"), FirstName = "Jean", LastName = "Andria", Role = UserRole.Teacher },
            new User { Email = "prof.francais@ecole.mg", PasswordHash = PasswordHasher.Hash("Password123!"), FirstName = "Voahangy", LastName = "Rasoa", Role = UserRole.Teacher },
        };
        context.Users.AddRange(teacherUsers);

        var teachers = new[]
        {
            new Teacher { User = teacherUsers[0], Specialty = "Mathématiques", HireDate = DateTime.UtcNow.AddYears(-4) },
            new Teacher { User = teacherUsers[1], Specialty = "Français", HireDate = DateTime.UtcNow.AddYears(-2) },
        };
        context.Teachers.AddRange(teachers);

        context.TeacherSchools.AddRange(
            new TeacherSchool { Teacher = teachers[0], School = schoolLumiere },
            new TeacherSchool { Teacher = teachers[1], School = schoolLumiere },
            new TeacherSchool { Teacher = teachers[1], School = schoolGenie });

        // École par défaut explicite pour le professeur multi-école : évite de dépendre d'un tri
        // (alphabétique ou par date de rattachement) qui deviendrait ambigu en cas d'égalité.
        teacherUsers[1].LastActiveSchoolId = schoolLumiere.Id;

        var academicProgram = new AcademicProgram
        {
            Name = "Collège Général",
            Code = "COL-GEN",
            Description = "Programme du collège, du niveau 6ème à la 3ème.",
            School = schoolLumiere
        };
        context.AcademicPrograms.Add(academicProgram);

        // Programme isolé pour la 2ᵉ école, pour prouver le cloisonnement des données entre écoles.
        var academicProgramGenie = new AcademicProgram
        {
            Name = "Collège Général",
            Code = "COL-GEN",
            Description = "Programme du collège, du niveau 6ème à la 3ème.",
            School = schoolGenie
        };
        context.AcademicPrograms.Add(academicProgramGenie);

        var classes = new[]
        {
            new SchoolClass { Name = "6ème A", Level = "6ème", AcademicYear = academicYear, Program = academicProgram, Capacity = 35, HomeroomTeacher = teachers[0], School = schoolLumiere },
            new SchoolClass { Name = "5ème B", Level = "5ème", AcademicYear = academicYear, Program = academicProgram, Capacity = 35, HomeroomTeacher = teachers[1], School = schoolLumiere },
        };
        context.Classes.AddRange(classes);

        // Classe isolée dans la 2ᵉ école, pour prouver le cloisonnement des données entre écoles.
        context.Classes.Add(new SchoolClass
        {
            Name = "3ème C",
            Level = "3ème",
            AcademicYear = academicYearGenie,
            Program = academicProgramGenie,
            Capacity = 30,
            HomeroomTeacher = teachers[1],
            School = schoolGenie
        });

        var rooms = new[]
        {
            new Room { Name = "Salle 101", Capacity = 40, Building = "Bâtiment A", School = schoolLumiere },
            new Room { Name = "Salle 102", Capacity = 40, Building = "Bâtiment A", School = schoolLumiere },
        };
        context.Rooms.AddRange(rooms);

        var courses = subjects.Select(s => new Course
        {
            Name = s.Name,
            Code = s.Name[..Math.Min(3, s.Name.Length)].ToUpperInvariant(),
            Program = academicProgram,
            Subject = s,
            School = schoolLumiere
        }).ToList();
        context.Courses.AddRange(courses);

        foreach (var course in courses)
        {
            context.Topics.Add(new Topic { Course = course, Name = $"Introduction à {course.Name}", Order = 1 });
            context.Topics.Add(new Topic { Course = course, Name = $"Approfondissement — {course.Name}", Order = 2 });
        }

        var scheduleDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        for (var i = 0; i < courses.Count; i++)
        {
            var course = courses[i];
            var teacher = i % 2 == 0 ? teachers[0] : teachers[1];
            context.CourseSchedules.Add(new CourseSchedule
            {
                Course = course,
                Room = rooms[i % rooms.Length],
                Teacher = teacher,
                Class = classes[i % classes.Length],
                AcademicTerm = academicTerms[0],
                DayOfWeek = scheduleDays[i % scheduleDays.Length],
                StartTime = new TimeOnly(8 + (i % 4) * 2, 0),
                EndTime = new TimeOnly(9 + (i % 4) * 2, 0),
                School = schoolLumiere
            });
        }

        var gradingScale = new GradingScale { Name = "Barème standard", IsDefault = true, School = schoolLumiere };
        context.GradingScales.Add(gradingScale);
        context.GradingScaleIntervals.AddRange(
            new GradingScaleInterval { GradingScale = gradingScale, Grade = "A", MinScore = 16, MaxScore = 20 },
            new GradingScaleInterval { GradingScale = gradingScale, Grade = "B", MinScore = 14, MaxScore = 15.99m },
            new GradingScaleInterval { GradingScale = gradingScale, Grade = "C", MinScore = 12, MaxScore = 13.99m },
            new GradingScaleInterval { GradingScale = gradingScale, Grade = "D", MinScore = 10, MaxScore = 11.99m },
            new GradingScaleInterval { GradingScale = gradingScale, Grade = "E", MinScore = 0, MaxScore = 9.99m });

        var assessmentGroups = new[]
        {
            new AssessmentGroup { Name = "Devoirs", Weightage = 40, AcademicTerm = academicTerms[0], School = schoolLumiere },
            new AssessmentGroup { Name = "Compositions", Weightage = 60, AcademicTerm = academicTerms[0], School = schoolLumiere },
        };
        context.AssessmentGroups.AddRange(assessmentGroups);

        var mathsCourse = courses.First(c => c.Name == "Mathématiques");
        var assessmentPlan = new AssessmentPlan
        {
            Name = "Composition de Mathématiques — Trimestre 1",
            Course = mathsCourse,
            Class = classes[0],
            AcademicTerm = academicTerms[0],
            AssessmentGroup = assessmentGroups[1],
            GradingScale = gradingScale,
            MaxScore = 20,
            PlannedDate = DateTime.UtcNow.AddDays(-11),
            Status = AssessmentPlanStatus.Completed,
            School = schoolLumiere
        };
        context.AssessmentPlans.Add(assessmentPlan);
        context.AssessmentCriteria.AddRange(
            new AssessmentCriteria { AssessmentPlan = assessmentPlan, Name = "Écrit", MaxScore = 15 },
            new AssessmentCriteria { AssessmentPlan = assessmentPlan, Name = "Oral", MaxScore = 5 });

        var studentCategories = new[]
        {
            new StudentCategory { Name = "Standard", Description = "Scolarité classique", School = schoolLumiere },
            new StudentCategory { Name = "Boursier", Description = "Bénéficie d'une bourse d'études", School = schoolLumiere },
        };
        context.StudentCategories.AddRange(studentCategories);

        var studentBatch = new StudentBatch
        {
            Name = "Promotion 2025-2026",
            AcademicYear = academicYear,
            School = schoolLumiere,
            StartDate = academicYear.StartDate,
            EndDate = academicYear.EndDate
        };
        context.StudentBatches.Add(studentBatch);

        var studentNames = new (string First, string Last, Gender Gender)[]
        {
            ("Tojo", "Randria", Gender.Masculin),
            ("Fara", "Rasoanaivo", Gender.Feminin),
            ("Nomena", "Rakotomalala", Gender.Masculin),
            ("Miora", "Andriamampianina", Gender.Feminin),
            ("Iarivo", "Ravelojaona", Gender.Masculin),
            ("Sitraka", "Rabemananjara", Gender.Feminin),
            ("Fetra", "Rakotondrabe", Gender.Masculin),
            ("Ony", "Razafindrakoto", Gender.Feminin),
        };

        var students = new List<Student>();
        for (var i = 0; i < studentNames.Length; i++)
        {
            var (first, last, gender) = studentNames[i];
            students.Add(new Student
            {
                EnrollmentNumber = $"MAT-2026-{(i + 1):000}",
                FirstName = first,
                LastName = last,
                Gender = gender,
                DateOfBirth = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i * 17),
                EnrollmentDate = DateTime.UtcNow.AddMonths(-6),
                Class = classes[i % 2],
                StudentCategory = studentCategories[i % 2],
                StudentBatch = studentBatch
            });
        }
        context.Students.AddRange(students);

        context.ProgramEnrollments.AddRange(students.Select(s => new ProgramEnrollment
        {
            Student = s,
            Program = academicProgram,
            AcademicYear = academicYear,
            EnrollmentDate = s.EnrollmentDate,
            Status = EnrollmentStatus.Active,
            School = schoolLumiere
        }));

        var studentGroup = new StudentGroup
        {
            Name = "Club Sciences",
            GroupType = "Club",
            AcademicYear = academicYear,
            School = schoolLumiere,
            MaxSize = 20
        };
        context.StudentGroups.Add(studentGroup);
        context.StudentGroupMembers.AddRange(
            students.Take(3).Select(s => new StudentGroupMember { StudentGroup = studentGroup, Student = s }));

        context.StudentLogs.AddRange(
            new StudentLog
            {
                Student = students[0],
                LogDate = DateTime.UtcNow.AddDays(-5),
                LogType = "Académique",
                Description = "Excellent travail lors du dernier devoir de mathématiques.",
                RecordedByUserId = director.Id
            },
            new StudentLog
            {
                Student = students[1],
                LogDate = DateTime.UtcNow.AddDays(-2),
                LogType = "Comportement",
                Description = "A aidé un camarade en difficulté pendant le cours de sciences.",
                RecordedByUserId = director.Id
            });

        var parentUsers = students.Select((s, i) => new User
        {
            Email = $"parent{i + 1}@ecole.mg",
            PasswordHash = PasswordHasher.Hash("Password123!"),
            FirstName = $"Parent{i + 1}",
            LastName = s.LastName,
            Role = UserRole.Parent
        }).ToList();
        context.Users.AddRange(parentUsers);

        for (var i = 0; i < students.Count; i++)
        {
            context.StudentParents.Add(new StudentParent
            {
                Student = students[i],
                ParentUser = parentUsers[i],
                Relationship = "Parent"
            });
        }

        var studentPortalUser = new User
        {
            Email = "eleve1@ecole.mg",
            PasswordHash = PasswordHasher.Hash("Password123!"),
            FirstName = students[0].FirstName,
            LastName = students[0].LastName,
            Role = UserRole.Student
        };
        context.Users.Add(studentPortalUser);
        students[0].User = studentPortalUser;

        context.StudentSiblings.Add(new StudentSibling { Student = students[0], SiblingStudent = students[5] });

        var sharedGuardian = new Guardian
        {
            FirstName = "Herizo",
            LastName = "Randria",
            Phone = "034 22 111 00",
            Email = "herizo.randria@example.mg",
            Occupation = "Ingénieur"
        };
        context.Guardians.Add(sharedGuardian);
        context.StudentGuardians.AddRange(
            new StudentGuardian { Student = students[0], Guardian = sharedGuardian, Relationship = "Père", IsPrimaryContact = true },
            new StudentGuardian { Student = students[5], Guardian = sharedGuardian, Relationship = "Père", IsPrimaryContact = true });

        var otherGuardian = new Guardian
        {
            FirstName = "Voninavoko",
            LastName = "Rasoanaivo",
            Phone = "034 33 222 11",
            Email = null,
            Occupation = "Commerçante"
        };
        context.Guardians.Add(otherGuardian);
        context.StudentGuardians.Add(new StudentGuardian { Student = students[1], Guardian = otherGuardian, Relationship = "Mère", IsPrimaryContact = true });

        var random = new Random(42);
        const string term = "Trimestre 1";

        foreach (var student in students)
        {
            var teacher = student.Class == classes[0] ? teachers[0] : teachers[1];

            foreach (var subject in subjects)
            {
                for (var g = 0; g < 2; g++)
                {
                    context.Grades.Add(new Grade
                    {
                        Student = student,
                        Subject = subject,
                        Teacher = teacher,
                        Class = student.Class,
                        Score = Math.Round((decimal)(random.NextDouble() * 12 + 8), 1),
                        MaxScore = 20,
                        Coefficient = subject.Coefficient,
                        Type = g == 0 ? EvaluationType.Devoir : EvaluationType.Composition,
                        Term = term,
                        EvaluatedAt = DateTime.UtcNow.AddDays(-random.Next(5, 60))
                    });
                }
            }

            var paymentStatus = random.Next(3) switch { 0 => PaymentStatus.Paye, 1 => PaymentStatus.EnAttente, _ => PaymentStatus.EnRetard };

            context.Payments.Add(new Payment
            {
                Student = student,
                Description = "Frais de scolarité — Trimestre 1",
                Amount = 250000,
                AcademicYear = "2025-2026",
                Term = term,
                DueDate = DateTime.UtcNow.AddDays(-10),
                Status = paymentStatus,
                PaidAt = paymentStatus == PaymentStatus.Paye ? DateTime.UtcNow.AddDays(-random.Next(1, 9)) : null,
                Method = "Mobile Money",
                InvoiceNumber = $"INV-2026-{student.EnrollmentNumber[^3..]}"
            });

            context.Attendances.Add(new Attendance
            {
                Student = student,
                Class = student.Class,
                Date = DateTime.UtcNow.Date,
                Status = random.Next(6) == 0 ? AttendanceStatus.Absent : AttendanceStatus.Present,
                RecordedByUserId = director.Id
            });
        }

        context.StudentLeaveApplications.AddRange(
            new StudentLeaveApplication
            {
                Student = students[2],
                School = schoolLumiere,
                StartDate = DateTime.UtcNow.Date.AddDays(3),
                EndDate = DateTime.UtcNow.Date.AddDays(5),
                Reason = "Consultation médicale programmée.",
                Status = LeaveApplicationStatus.Pending,
                RequestedByUserId = parentUsers[2].Id
            },
            new StudentLeaveApplication
            {
                Student = students[4],
                School = schoolLumiere,
                StartDate = DateTime.UtcNow.Date.AddDays(-10),
                EndDate = DateTime.UtcNow.Date.AddDays(-8),
                Reason = "Voyage familial.",
                Status = LeaveApplicationStatus.Approved,
                RequestedByUserId = parentUsers[4].Id,
                DecisionDate = DateTime.UtcNow.AddDays(-12),
                DecisionNotes = "Autorisé."
            });

        var feeCategories = new[]
        {
            new FeeCategory { Name = "Scolarité", Description = "Frais de scolarité de base" },
            new FeeCategory { Name = "Cantine", Description = "Restauration scolaire" },
            new FeeCategory { Name = "Transport", Description = "Ramassage scolaire" },
        };
        context.FeeCategories.AddRange(feeCategories);

        var feeStructure = new FeeStructure { Name = "Frais standard 2025-2026", AcademicYear = academicYear };
        context.FeeStructures.Add(feeStructure);
        context.FeeStructureItems.AddRange(
            new FeeStructureItem { FeeStructure = feeStructure, FeeCategory = feeCategories[0], Amount = 200000 },
            new FeeStructureItem { FeeStructure = feeStructure, FeeCategory = feeCategories[1], Amount = 40000 },
            new FeeStructureItem { FeeStructure = feeStructure, FeeCategory = feeCategories[2], Amount = 10000 });

        var feeSchedule = new FeeSchedule
        {
            FeeStructure = feeStructure,
            AcademicTerm = academicTerms[0],
            DueDate = academicTerms[0].StartDate.AddDays(30)
        };
        context.FeeSchedules.Add(feeSchedule);

        var paidInvoice = new Invoice
        {
            Student = students[0],
            FeeSchedule = feeSchedule,
            InvoiceNumber = $"FAC-2025T1-{students[0].EnrollmentNumber[^3..]}",
            TotalAmount = 250000,
            DueDate = feeSchedule.DueDate,
            Status = PaymentStatus.Paye
        };
        var pendingInvoice = new Invoice
        {
            Student = students[1],
            FeeSchedule = feeSchedule,
            InvoiceNumber = $"FAC-2025T1-{students[1].EnrollmentNumber[^3..]}",
            TotalAmount = 250000,
            DueDate = feeSchedule.DueDate,
            Status = PaymentStatus.EnAttente
        };
        context.Invoices.AddRange(paidInvoice, pendingInvoice);

        context.Payments.Add(new Payment
        {
            Student = students[0],
            Invoice = paidInvoice,
            Description = "Frais standard 2025-2026 — Trimestre 1",
            Amount = 250000,
            AcademicYear = "2025-2026",
            Term = term,
            DueDate = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow.AddDays(-3),
            Status = PaymentStatus.Paye,
            Method = "Mobile Money"
        });

        context.StudentApplicants.AddRange(
            new StudentApplicant
            {
                FirstName = "Hery",
                LastName = "Andriamanjato",
                DateOfBirth = new DateTime(2014, 3, 12, 0, 0, 0, DateTimeKind.Utc),
                Gender = Gender.Masculin,
                GuardianName = "Parent d'Hery",
                GuardianPhone = "034 00 000 01",
                LevelAppliedFor = "6ème",
                AcademicYear = academicYear,
                School = schoolLumiere,
                Status = AdmissionStatus.Submitted
            },
            new StudentApplicant
            {
                FirstName = "Voninkazo",
                LastName = "Rasolofo",
                DateOfBirth = new DateTime(2013, 7, 22, 0, 0, 0, DateTimeKind.Utc),
                Gender = Gender.Feminin,
                GuardianName = "Parent de Voninkazo",
                GuardianPhone = "034 00 000 02",
                LevelAppliedFor = "5ème",
                AcademicYear = academicYear,
                School = schoolLumiere,
                Status = AdmissionStatus.UnderReview
            },
            new StudentApplicant
            {
                FirstName = "Tahina",
                LastName = "Rakotoson",
                DateOfBirth = new DateTime(2014, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                Gender = Gender.Masculin,
                GuardianName = "Parent de Tahina",
                GuardianPhone = "034 00 000 03",
                LevelAppliedFor = "6ème",
                AcademicYear = academicYear,
                School = schoolLumiere,
                Status = AdmissionStatus.Rejected,
                DecisionDate = DateTime.UtcNow.AddDays(-3),
                DecisionNotes = "Places déjà complètes pour ce niveau."
            });

        await context.SaveChangesAsync();
    }
}
