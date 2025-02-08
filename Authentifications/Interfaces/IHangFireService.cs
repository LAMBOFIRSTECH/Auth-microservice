namespace Authentifications.Interfaces;
public interface IHangFireService
{
    void ScheduleRetrieveDataFromExternalApi(bool result);
}
