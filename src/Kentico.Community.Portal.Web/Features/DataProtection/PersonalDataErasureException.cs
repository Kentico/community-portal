namespace Kentico.Community.Portal.Web.Features.DataProtection;

/// <summary>
/// An erasure step failed. Thrown out of the eraser so the Data protection app reports the erasure
/// as failed (it logs the exception to the event log). Messages name IDs only, never personal data,
/// and say whether the erasure can be re-run.
/// </summary>
public class PersonalDataErasureException(string message, Exception innerException) : Exception(message, innerException);
