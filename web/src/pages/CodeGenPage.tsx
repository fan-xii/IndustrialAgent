import { useState } from 'react';
import Editor from '@monaco-editor/react';
import { api } from '../api/client';
import type { CodeGenResponse } from '../api/client';

const codeTypes = [
  { value: 'ViewModel', label: 'ViewModel', desc: 'Prism MVVM 视图模型' },
  { value: 'Service', label: 'Service', desc: '服务接口 + 实现' },
  { value: 'UnitTest', label: 'UnitTest', desc: 'xUnit + Moq 单元测试' },
];

export default function CodeGenPage() {
  const [type, setType] = useState('ViewModel');
  const [requirement, setRequirement] = useState('');
  const [moduleName, setModuleName] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<CodeGenResponse | null>(null);
  const [error, setError] = useState('');

  const handleGenerate = async () => {
    if (!requirement.trim() || !moduleName.trim()) return;
    setLoading(true);
    setError('');
    setResult(null);
    try {
      const data = await api.codeGen.generate(type, requirement, moduleName);
      setResult(data);
    } catch (e: any) {
      setError(e.response?.data || e.message || '生成失败');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-8 max-w-6xl mx-auto">
      <header className="mb-6">
        <h2 className="text-2xl font-bold text-white">规范化代码生成</h2>
        <p className="text-industrial-400 mt-1">遵循 Prism MVVM 架构，生成 ViewModel、服务、单元测试</p>
      </header>

      <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800 mb-6">
        <div className="grid grid-cols-3 gap-3 mb-4">
          {codeTypes.map(t => (
            <button
              key={t.value}
              onClick={() => setType(t.value)}
              className={`p-4 rounded-lg border text-left transition-colors ${
                type === t.value
                  ? 'border-industrial-500 bg-industrial-800 text-white'
                  : 'border-industrial-700 bg-industrial-950 text-industrial-300 hover:border-industrial-600'
              }`}
            >
              <div className="font-medium">{t.label}</div>
              <div className="text-xs text-industrial-400 mt-1">{t.desc}</div>
            </button>
          ))}
        </div>

        <div className="grid grid-cols-3 gap-4">
          <div className="col-span-1">
            <label className="block text-sm font-medium text-industrial-300 mb-2">模块名称</label>
            <input
              value={moduleName}
              onChange={e => setModuleName(e.target.value)}
              placeholder="如：DeviceMonitor"
              className="w-full bg-industrial-950 border border-industrial-700 rounded-lg px-4 py-2.5 text-white placeholder-industrial-500 focus:outline-none focus:border-industrial-500"
            />
          </div>
          <div className="col-span-2">
            <label className="block text-sm font-medium text-industrial-300 mb-2">业务需求</label>
            <input
              value={requirement}
              onChange={e => setRequirement(e.target.value)}
              placeholder="如：设备状态监控，显示温度/压力，支持启停控制"
              className="w-full bg-industrial-950 border border-industrial-700 rounded-lg px-4 py-2.5 text-white placeholder-industrial-500 focus:outline-none focus:border-industrial-500"
            />
          </div>
        </div>

        <button
          onClick={handleGenerate}
          disabled={loading || !requirement.trim() || !moduleName.trim()}
          className="mt-4 px-6 py-2.5 bg-industrial-600 hover:bg-industrial-500 disabled:bg-industrial-800 disabled:text-industrial-500 text-white rounded-lg font-medium"
        >
          {loading ? '生成中...' : '生成代码'}
        </button>
      </div>

      {error && <div className="mb-4 p-4 bg-red-900/30 border border-red-700 rounded-lg text-red-300">{error}</div>}

      {result && (
        <div className="bg-industrial-900 rounded-lg border border-industrial-800 overflow-hidden">
          <div className="flex items-center justify-between px-4 py-2 bg-industrial-800 border-b border-industrial-700">
            <span className="text-sm text-industrial-300 font-mono">{result.targetPath}</span>
            <button
              onClick={() => navigator.clipboard.writeText(result.code)}
              className="text-xs px-3 py-1 bg-industrial-700 hover:bg-industrial-600 text-white rounded"
            >
              复制
            </button>
          </div>
          <Editor
            height="500px"
            language={result.language}
            value={result.code}
            theme="vs-dark"
            options={{ readOnly: true, fontSize: 13, minimap: { enabled: false } }}
          />
        </div>
      )}
    </div>
  );
}
