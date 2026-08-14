using GameFramework.Event;

/// <summary>
/// 全局运行数据
/// 注意：依赖注入
/// </summary>
public class Blackboard
{
    public CharacterRunTimeData characterRunTimeData;
    public InputData inputData;
    public ReadyToApply readytoApply;
    public CharacterSO CharacterSO;
    public ModificationManager modificationManager;
    public EventBus eventBus;
    public ComboManager comboManager;
    public Blackboard(CharacterRunTimeData _playerRunTimeData, InputData _inputData, ReadyToApply _expectedState,
    CharacterSO _CharacterSO,ModificationManager modification,EventBus _event)
    {
        characterRunTimeData = _playerRunTimeData;
        inputData = _inputData;
        readytoApply = _expectedState;
        CharacterSO = _CharacterSO;
        modificationManager = modification;
        eventBus = _event;
    }

    bool CheckInputData(ECommandType eCommandType)
    {
        ECommand command = inputData.PeekBufferComand();
        if (command.eCommand==eCommandType)
        {
            // inputData.UseBufferCommand();
            return true;
        }
        return false;
    }
}
