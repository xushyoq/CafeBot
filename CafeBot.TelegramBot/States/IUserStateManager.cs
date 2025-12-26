namespace CafeBot.TelegramBot.States;

public interface IUserStateManager
{
    UserState GetState(long userId);
    void SetState(long userId, UserState state);
    UserStateData GetStateData(long userId); // Переименовано GetData в GetStateData
    void SetData(long userId, UserStateData data);
    void ClearState(long userId);
    void ClearStateData(long userId); // Добавлен новый метод
}