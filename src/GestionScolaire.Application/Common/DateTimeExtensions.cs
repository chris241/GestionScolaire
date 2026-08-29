namespace GestionScolaire.Application.Common;

public static class DateTimeExtensions
{
    /// Les DateTime désérialisés depuis une requête JSON arrivent en Kind=Unspecified quand le client
    /// n'envoie pas d'offset explicite ; Npgsql refuse d'écrire un Kind autre qu'Utc dans une colonne
    /// "timestamp with time zone". À appeler sur toute date reçue d'une requête avant de l'assigner à une entité.
    public static DateTime AsUtc(this DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
