import { useState } from 'react';
import { api } from '../api/client';
import type { CodeQaResponse } from '../api/client';

export default function CodeQaPage() {
  const [question, setQuestion] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<CodeQaResponse | null>(null);
  const [error, setError] = useState('');

  const handleAsk = async () => {
    if (!question.trim()) return;
    setLoading(true);
    setError('');
    setResult(null);
    try {
      const data = await api.codeQa.ask(question);
      setResult(data);
    } catch (e: any) {
      setError(e.response?.data || e.message || '请求失败');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-8 max-w-5xl mx-auto">
      <header className="mb-6">
        <h2 className="text-2xl font-bold text-white">代码智能问答</h2>
        <p className="text-industrial-400 mt-1">基于整仓代码索引，回答代码定位、实现逻辑等问题</p>
      </header>

      <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800">
        <label className="block text-sm font-medium text-industrial-300 mb-2">输入你的问题</label>
        <textarea
          value={question}
          onChange={e => setQuestion(e.target.value)}
          placeholder="例如：报警逻辑在哪实现？Modbus 通信如何封装？"
          className="w-full h-24 bg-industrial-950 border border-industrial-700 rounded-lg px-4 py-3 text-white placeholder-industrial-500 focus:outline-none focus:border-industrial-500 resize-none"
        />
        <button
          onClick={handleAsk}
          disabled={loading || !question.trim()}
          className="mt-3 px-6 py-2.5 bg-industrial-600 hover:bg-industrial-500 disabled:bg-industrial-800 disabled:text-industrial-500 text-white rounded-lg font-medium transition-colors"
        >
          {loading ? '检索中...' : '提问'}
        </button>
      </div>

      {error && (
        <div className="mt-4 p-4 bg-red-900/30 border border-red-700 rounded-lg text-red-300">
          {error}
        </div>
      )}

      {result && (
        <div className="mt-6 space-y-4">
          <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800">
            <h3 className="text-sm font-medium text-industrial-400 mb-3">回答</h3>
            <div className="prose prose-invert max-w-none">
              <pre className="whitespace-pre-wrap text-sm text-industrial-100 font-sans leading-relaxed">
                {result.answer}
              </pre>
            </div>
          </div>

          {result.references.length > 0 && (
            <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800">
              <h3 className="text-sm font-medium text-industrial-400 mb-3">
                引用代码片段 ({result.references.length})
              </h3>
              <div className="space-y-3">
                {result.references.map((ref, i) => (
                  <div key={i} className="bg-industrial-950 rounded-lg p-4 border border-industrial-800">
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-xs text-industrial-400">
                        {ref.metadata.filePath || '未知文件'}
                        {ref.metadata.startLine && ` : ${ref.metadata.startLine}-${ref.metadata.endLine}`}
                      </span>
                      <span className="text-xs text-industrial-500">
                        相关度: {(ref.score * 100).toFixed(1)}%
                      </span>
                    </div>
                    <pre className="text-xs text-industrial-200 font-mono overflow-x-auto">
                      {ref.content.slice(0, 500)}
                      {ref.content.length > 500 && '\n...'}
                    </pre>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
