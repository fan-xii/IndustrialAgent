import { useState } from 'react';
import { api } from '../api/client';

export default function LogDiagnosePage() {
  const [logContent, setLogContent] = useState('');
  const [stackTrace, setStackTrace] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState('');
  const [error, setError] = useState('');

  const handleAnalyze = async () => {
    if (!logContent.trim()) return;
    setLoading(true);
    setError('');
    setResult('');
    try {
      const data = await api.logDiagnose.analyze(logContent, stackTrace || undefined);
      setResult(data.rootCause);
    } catch (e: any) {
      setError(e.response?.data || e.message || '分析失败');
    } finally {
      setLoading(false);
    }
  };

  const loadSample = () => {
    setLogContent(`[2026-06-18 10:23:45] [ERROR] 设备通信异常
System.NullReferenceException: Object reference not set to an instance of an object.
   at IndustrialAgent.Services.ModbusService.ReadHoldingRegisters(Int32 address, Int32 count) in D:\\Projects\\IndustrialAgent\\Services\\ModbusService.cs:line 87
   at IndustrialAgent.ViewModels.DeviceMonitorViewModel.RefreshData() in D:\\Projects\\IndustrialAgent\\ViewModels\\DeviceMonitorViewModel.cs:line 45
   at IndustrialAgent.ViewModels.DeviceMonitorViewModel.<.ctor>b__3_0() in D:\\Projects\\IndustrialAgent\\ViewModels\\DeviceMonitorViewModel.cs:line 28`);
  };

  return (
    <div className="p-8 max-w-5xl mx-auto">
      <header className="mb-6">
        <h2 className="text-2xl font-bold text-white">日志故障排查</h2>
        <p className="text-industrial-400 mt-1">上传运行日志和异常堆栈，自动定位根因、给出修复方案</p>
      </header>

      <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800 mb-6">
        <div className="flex items-center justify-between mb-2">
          <label className="text-sm font-medium text-industrial-300">日志内容</label>
          <button onClick={loadSample} className="text-xs text-industrial-400 hover:text-industrial-200 underline">
            加载示例
          </button>
        </div>
        <textarea
          value={logContent}
          onChange={e => setLogContent(e.target.value)}
          placeholder="粘贴程序运行日志..."
          className="w-full h-40 bg-industrial-950 border border-industrial-700 rounded-lg px-4 py-3 text-white placeholder-industrial-500 focus:outline-none focus:border-industrial-500 resize-none font-mono text-sm"
        />

        <label className="block text-sm font-medium text-industrial-300 mt-4 mb-2">异常堆栈（可选）</label>
        <textarea
          value={stackTrace}
          onChange={e => setStackTrace(e.target.value)}
          placeholder="粘贴异常堆栈信息（如日志中已包含可留空）..."
          className="w-full h-24 bg-industrial-950 border border-industrial-700 rounded-lg px-4 py-3 text-white placeholder-industrial-500 focus:outline-none focus:border-industrial-500 resize-none font-mono text-sm"
        />

        <button
          onClick={handleAnalyze}
          disabled={loading || !logContent.trim()}
          className="mt-4 px-6 py-2.5 bg-industrial-600 hover:bg-industrial-500 disabled:bg-industrial-800 disabled:text-industrial-500 text-white rounded-lg font-medium"
        >
          {loading ? '分析中...' : '开始诊断'}
        </button>
      </div>

      {error && <div className="mb-4 p-4 bg-red-900/30 border border-red-700 rounded-lg text-red-300">{error}</div>}

      {result && (
        <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800">
          <h3 className="text-sm font-medium text-industrial-400 mb-3">诊断报告</h3>
          <pre className="whitespace-pre-wrap text-sm text-industrial-100 font-sans leading-relaxed">{result}</pre>
        </div>
      )}
    </div>
  );
}
