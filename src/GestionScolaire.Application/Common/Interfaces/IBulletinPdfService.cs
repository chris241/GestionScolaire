using GestionScolaire.Application.DTOs.Grades;

namespace GestionScolaire.Application.Common.Interfaces;

public interface IBulletinPdfService
{
    byte[] GenerateBulletin(BulletinDto bulletin);
}
