# LyricFX框架优化计划

## 当前存在的问题

### 1. 时间同步问题
- **累积误差**: 当前实现中使用相对时间计算，即使是毫秒级的细微误差也会随着播放时间延长而累积，导致歌词与音频逐渐不同步。
- **手动停止时的负值时间戳**: 当手动停止歌词播放时，如果`playSessionStartTime`未正确初始化，会出现负值的会话持续时间。
- **处理延迟**: 系统处理、UI渲染和对象创建会引入额外延迟，特别是在第一行歌词显示时更为明显。

### 2. 性能考虑
- **实时对象创建**: 当前在每行歌词播放时才创建相应对象，增加了实时处理负担。
- **调试日志开销**: 大量调试信息记录虽然有助于调试，但在生产环境可能影响性能。
- **效果配置的反射使用**: 使用反射获取效果配置对象可能带来性能开销。

### 3. 鲁棒性问题
- **异常处理**: 部分场景下的异常处理可以更加完善，如LRC解析失败或格式不符合预期时的行为。
- **资源清理**: 在歌词播放中断或应用切换场景时，需确保所有资源和协程正确清理。

## 后续优化建议

### 1. 时间同步优化
- **使用绝对时间计算**: 
  ```csharp
  float nextAbsoluteTime = startTime + (float)lyrics[i + 1].TimeStamp + syncOffset;
  float waitDuration = nextAbsoluteTime - Time.realtimeSinceStartup;
  ```

- **周期性重新同步机制**: 
  ```csharp
  // 每N行歌词检查一次累积误差
  if (i % 5 == 0) {
      float expectedElapsedTime = (float)line.TimeStamp + syncOffset;
      float actualElapsedTime = Time.realtimeSinceStartup - startTime;
      float drift = actualElapsedTime - expectedElapsedTime;
      
      if (Math.Abs(drift) > 0.02f) {
          // 调整临时同步偏移值以补偿漂移
          tempSyncOffset = -drift;
          LyricFXDebugger.Instance.RecordTimePoint($"重新同步: 检测到漂移 {drift:F3}s, 应用临时校正 {tempSyncOffset:F3}s");
      }
  }
  ```

- **手动停止时间计算修复**:
  ```csharp
  // 确保playSessionStartTime已被正确设置
  if (playSessionStartTime > 0) {
      float sessionDuration = Time.realtimeSinceStartup - playSessionStartTime;
      LyricFXDebugger.Instance.RecordTimePoint($"歌词播放会话结束, 持续时间: {sessionDuration:F3}s");
  } else {
      LyricFXDebugger.Instance.RecordTimePoint("歌词播放会话结束 (未开始播放就停止)");
  }
  ```

### 2. 性能优化
- **对象池化**:
  为歌词行对象实现简单的对象池，减少垃圾回收压力和实时创建开销。
  
- **预创建机制**:
  ```csharp
  // 播放开始前预创建歌词行对象
  private void PreCreateLyricLines(List<LrcLine> lyrics, string effectId) {
      // 至少预创建前N行
      int preCreateCount = Math.Min(5, lyrics.Count); 
      for (int i = 0; i < preCreateCount; i++) {
          CreateLyricLine(lyrics[i].Text, effectId, false);
      }
  }
  ```

- **配置缓存**:
  ```csharp
  // 缓存效果配置以减少反射调用
  private Dictionary<string, object> configCache = new Dictionary<string, object>();
  
  private object GetConfigForEffect(string effectId, float availableDuration, int characterCount) {
      string cacheKey = $"{effectId}_{availableDuration}_{characterCount}";
      if (configCache.TryGetValue(cacheKey, out var cachedConfig)) {
          return cachedConfig;
      }
      
      // 原有的配置获取逻辑...
      
      // 缓存结果
      configCache[cacheKey] = config;
      return config;
  }
  ```

### 3. 鲁棒性增强
- **增强异常处理**:
  改进解析失败、配置无效等情况下的错误处理和日志记录。

- **完善资源清理**:
  确保在各种场景切换和异常情况下正确释放资源。
  
- **自动重试机制**:
  对于非致命性错误添加自动重试逻辑。

### 4. 其他功能增强
- **音频同步接口**:
  提供与音频播放器直接集成的接口，使用实际音频播放位置作为同步基准点。
  
- **可视化调试工具**:
  开发简单的编辑器工具，可视化显示时间线和同步状态。
  
- **调试开关分层**:
  细化调试开关，允许只记录特定类型的调试信息，减少非必要日志。

## 优先级排序
1. **高优先级**: 
   - 绝对时间计算实现
   - 周期性重新同步机制
   - 手动停止时间计算修复
   
2. **中优先级**:
   - 预创建机制
   - 增强异常处理
   - 资源清理完善
   
3. **低优先级**:
   - 对象池化
   - 配置缓存
   - 可视化调试工具
