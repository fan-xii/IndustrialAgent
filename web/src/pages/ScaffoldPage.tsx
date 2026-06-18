import { useState } from 'react';
import { api } from '../api/client';
import type { ScaffoldPlanResponse, ScaffoldExecuteResponse } from '../api/client';

export default function ScaffoldPage() {
  const [requirement, setRequirement] = useState('');
  const [loading, setLoading] = useState(false);
  const [plan, setPlan] = useState<ScaffoldPlanResponse | null>(null);
  const [targetDir, setTargetDir] = useState('');
  const [executing, setExecuting] = useState(false);
  const [execResult, setExecResult] = useState<ScaffoldExecuteResponse | null>(null);
  const [error, setError] = useState('');

  const handlePlan = async () => {
    if (!requirement.trim()) return;
    setLoading(true);
    setError('');
    setPlan(null);
    setExecResult(null);
    try {
      const data = await api.scaffold.plan(requirement);
      setPlan(data);
    } catch (e: any) {
      setError(e.response?.data || e.message || '规划失败');
    } finally {
      setLoading(false);
    }
  };

  const handleExecute = async () => {
    if (!plan || !targetDir.trim()) return;
    setExecuting(true);
    setError('');
    try {
      const data = await api.scaffold.execute(targetDir, plan);
      setExecResult(data);
    } catch (e: any) {
      setError(e.response?.data || e.message || '执行失败');
    } finally {
      setExecuting(false);
    }
  };

  return (
    <div className="p-8 max-w-5xl mx-auto">
      <header className="mb-6">
        <h2 className="text-2xl font-bold text-white">项目脚手架生成</h2>
        <p className="text-industrial-400 mt-1">输入业务需求，一键生成符合 Prism MVVM 规范的模块结构</p>
      </header>

      <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800 mb-6">
        <label className="block text-sm font-medium text-industrial-300 mb-2">业务需求</label>
        <textarea
          value={requirement}
          onChange={e => setRequirement(e.target.value)}
          placeholder="例如：温度采集模块，支持 8 通道热电偶采集、实时曲线显示、数据存储"
          className="w-full h-24 bg-industrial-950 border border-industrial-700 rounded-lg px-4 py-3 text-white placeholder-industrial-500 focus:outline-none focus:border-industrial-500 resize-none"
        />
        <button
          onClick={handlePlan}
          disabled={loading || !requirement.trim()}
          className="mt-3 px-6 py-2.5 bg-industrial-600 hover:bg-industrial-500 disabled:bg-industrial-800 disabled:text-industrial-500 text-white rounded-lg font-medium"
        >
          {loading ? '规划中...' : '生成方案'}
        </button>
      </div>

      {error && <div className="mb-4 p-4 bg-red-900/30 border border-red-700 rounded-lg text-red-300">{error}</div>}

      {plan && (
        <div className="space-y-4">
          <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-lg font-bold text-white">{plan.moduleName}</h3>
                <p className="text-sm text-industrial-400 mt-1">{plan.summary}</p>
              </div>
              <span className="text-xs px-3 py-1 bg-industrial-700 text-industrial-200 rounded-full">
                {plan.files.length} 个文件
              </span>
            </div>

            <div className="space-y-2">
              {plan.files.map((file, i) => (
                <details key={i} className="bg-industrial-950 rounded-lg border border-industrial-800">
                  <summary className="px-4 py-2.5 cursor-pointer text-sm text-industrial-200 hover:bg-industrial-800 rounded-lg">
                    <span className="font-mono text-industrial-300">{file.path}</span>
                    <span className="ml-2 text-xs text-industrial-500">[{file.template}]</span>
                  </summary>
                  <pre className="px-4 py-3 text-xs text-industrial-300 font-mono overflow-x-auto border-t border-industrial-800">
                    {file.content.slice(0, 1000)}
                    {file.content.length > 1000 && '\n...'}
                  </pre>
                </details>
              ))}
            </div>
          </div>

          <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800">
            <label className="block text-sm font-medium text-industrial-300 mb-2">目标目录（绝对路径）</label>
            <div className="flex gap-3">
              <input
                value={targetDir}
                onChange={e => setTargetDir(e.target.value)}
                placeholder="如：D:\Projects\MyIndustrialApp\src\Modules"
                className="flex-1 bg-industrial-950 border border-industrial-700 rounded-lg px-4 py-2.5 text-white placeholder-industrial-500 focus:outline-none focus:border-industrial-500"
              />
              <button
                onClick={handleExecute}
                disabled={executing || !targetDir.trim()}
                className="px-6 py-2.5 bg-green-700 hover:bg-green-600 disabled:bg-industrial-800 disabled:text-industrial-500 text-white rounded-lg font-medium"
              >
                {executing ? '生成中...' : '执行生成'}
              </button>
            </div>
          </div>

          {execResult && (
            <div className="bg-green-900/20 rounded-lg p-6 border border-green-700">
              <h3 className="text-sm font-medium text-green-400 mb-3">生成成功</h3>
              <div className="space-y-1 mb-4">
                {execResult.createdFiles.map((f, i) => (
                  <div key={i} className="text-sm text-industrial-200 font-mono">✓ {f}</div>
                ))}
              </div>
              <div className="mt-4">
                <p className="text-xs text-industrial-400 mb-2">DI 注册代码（添加到 App.xaml.cs）：</p>
                <pre className="text-xs text-industrial-300 font-mono bg-industrial-950 p-3 rounded-lg border border-industrial-800">
                  {execResult.diRegistrationSnippet}
                </pre>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
