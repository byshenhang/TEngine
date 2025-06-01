using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameLogic;
using TEngine;
using UnityEngine;

/// <summary>
/// u6218u6597u6d41u7a0b - u7ba1u7406VRu6218u6597u76f8u5173u7684u751fu547du5468u671f
/// </summary>
public class ProcedureCombat : ProcedureBase
{
    // u6218u6597u76f8u5173u914du7f6e
    private EntityData _playerData;
    private List<EntityData> _enemyDataList;
    private PlayerEntity _playerEntity;
    private List<EnemyEntity> _enemyEntities = new List<EnemyEntity>();

    /// <summary>
    /// u8fdbu5165u6218u6597u6d41u7a0b
    /// </summary>
    protected override void OnEnter(IFsm<IProcedureModule> procedureOwner)
    {
        base.OnEnter(procedureOwner);

        // u521du59cbu5316u6218u6597u6570u636e
        Log.Info("[ProcedureCombat] u8fdbu5165u6218u6597u6d41u7a0b");

        // u542fu52a8u6218u6597u521du59cbu5316
        InitCombat().Forget();
    }

    /// <summary>
    /// u79bbu5f00u6218u6597u6d41u7a0b
    /// </summary>
    protected override void OnLeave(IFsm<IProcedureModule> procedureOwner, bool isShutdown)
    {
        // u6e05u7406u6218u6597u8d44u6e90
        CleanupCombat();

        Log.Info("[ProcedureCombat] u79bbu5f00u6218u6597u6d41u7a0b");

        base.OnLeave(procedureOwner, isShutdown);
    }

    /// <summary>
    /// u6218u6597u6d41u7a0bu66f4u65b0
    /// </summary>
    protected override void OnUpdate(IFsm<IProcedureModule> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

        // u6218u6597u6d41u7a0bu7279u6709u7684u66f4u65b0u903bu8f91
        CheckCombatState(procedureOwner);
    }

    /// <summary>
    /// u5f02u6b65u521du59cbu5316u6218u6597
    /// </summary>
    private async UniTaskVoid InitCombat()
    {
        await UniTask.Yield();

        // u7b49u5f85u52a0u8f7du6218u6597u8d44u6e90
        await LoadCombatResources();

        // u521bu5efau73a9u5bb6u5b9eu4f53
        CreatePlayerEntity();

        // u521bu5efau654cu4ebau5b9eu4f53
        CreateEnemyEntities();

        // u542fu52a8u6218u6597u72b6u6001
        GameModule.Combat.StartCombat();

        Log.Info("[ProcedureCombat] u6218u6597u521du59cbu5316u5b8cu6210");
    }

    /// <summary>
    /// u52a0u8f7du6218u6597u8d44u6e90
    /// </summary>
    private async UniTask LoadCombatResources()
    {
        // u6a21u62dfu52a0u8f7du8fc7u7a0b
        await UniTask.Delay(500);

        // u521du59cbu5316u73a9u5bb6u6570u636e
        _playerData = new EntityData
        {
            Name = "Player",
            Position = Vector3.zero,
            Rotation = Quaternion.identity
        };

        // u8bbeu7f6eu73a9u5bb6u57fau7840u5c5eu6027
        _playerData.SetAttribute(AttributeType.MaxHealth, 100);
        _playerData.SetAttribute(AttributeType.Health, 100);
        _playerData.SetAttribute(AttributeType.Attack, 20);
        _playerData.SetAttribute(AttributeType.Defense, 10);

        // u521du59cbu5316u654cu4ebau6570u636eu5217u8868
        _enemyDataList = new List<EntityData>();

        // u6dfbu52a0u793au4f8bu654cu4eba
        var enemyData = new EntityData
        {
            Name = "Enemy1",
            Position = new Vector3(0, 0, 5),
            Rotation = Quaternion.Euler(0, 180, 0)
        };

        // u8bbeu7f6eu654cu4ebau57fau7840u5c5eu6027
        enemyData.SetAttribute(AttributeType.MaxHealth, 50);
        enemyData.SetAttribute(AttributeType.Health, 50);
        enemyData.SetAttribute(AttributeType.Attack, 15);
        enemyData.SetAttribute(AttributeType.Defense, 5);

        _enemyDataList.Add(enemyData);

        Log.Info("[ProcedureCombat] u6218u6597u8d44u6e90u52a0u8f7du5b8cu6210");
    }

    /// <summary>
    /// u521bu5efau73a9u5bb6u5b9eu4f53
    /// </summary>
    private void CreatePlayerEntity()
    {
        _playerEntity = GameModule.Combat.CreateEntity<PlayerEntity>(_playerData);
        Log.Info("[ProcedureCombat] u73a9u5bb6u5b9eu4f53u521bu5efau5b8cu6210");
    }

    /// <summary>
    /// u521bu5efau654cu4ebau5b9eu4f53
    /// </summary>
    private void CreateEnemyEntities()
    {
        _enemyEntities.Clear();

        foreach (var enemyData in _enemyDataList)
        {
            var enemy = GameModule.Combat.CreateEntity<EnemyEntity>(enemyData);
            _enemyEntities.Add(enemy);
        }

        Log.Info($"[ProcedureCombat] u521bu5efau4e86 {_enemyEntities.Count} u4e2au654cu4ebau5b9eu4f53");
    }

    /// <summary>
    /// u68c0u67e5u6218u6597u72b6u6001uff0cu51b3u5b9au662fu5426u9700u8981u5207u6362u6d41u7a0b
    /// </summary>
    private void CheckCombatState(IFsm<IProcedureModule> procedureOwner)
    {
        // u68c0u67e5u6218u6597u662fu5426u7ed3u675f
        if (GameModule.Combat.GetCombatState() == CombatStateType.Idle)
        {
            // u6218u6597u5df2u7ed3u675fuff0cu5207u6362u5230u5176u4ed6u6d41u7a0b
            // u8fd9u91ccu53efu4ee5u6839u636eu6218u6597u7ed3u679cu5207u6362u5230u4e0du540cu7684u6d41u7a0b
            // procedureOwner.SetData<VarInt>("CombatResult", 1); // 1u8868u793au80dcu5229uff0c0u8868u793au5931u8d25
            // ChangeState<ProcedureCombatResult>(procedureOwner);

            // u7b80u5355u8d77u89c1uff0cu8fd9u91ccu76f4u63a5u8fd4u56deu6e38u620fu6d41u7a0b
            //ChangeState<ProcedureStartGame>(procedureOwner);
        }
    }

    /// <summary>
    /// u6e05u7406u6218u6597u8d44u6e90
    /// </summary>
    private void CleanupCombat()
    {
        // u7ed3u675fu6218u6597u72b6u6001
        GameModule.Combat.EndCombat();

        // u6e05u7406u5b9eu4f53u5f15u7528
        _playerEntity = null;
        _enemyEntities.Clear();

        Log.Info("[ProcedureCombat] u6218u6597u8d44u6e90u6e05u7406u5b8cu6210");
    }
}
