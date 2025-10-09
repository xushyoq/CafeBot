namespace CafeBot.TelegramBot.States;

public interface IUserStateManager
{
    UserState GetState(long userId);
    void SetState(long userId, UserState state);
    UserStateData GetData(long userId);
    void SetData(long userId, UserStateData data);
    void ClearState(long userId);
}