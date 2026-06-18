import { useState } from 'react';
import { api } from '../api/client';

const protocols = ['Modbus', 'SECS_GEM', 'S7', 'EtherNetIP'];

export default function DocRagPage() {
  const [file, setFile] = useState<File | null>(null);
  const [protocol, setProtocol] = useState('Modbus');
  const [uploading, setUploading] = useState(false);
  const [uploadResult, setUploadResult] = useState('');
  const [question, setQuestion] = useState('');
  const [loading, setLoading] = useState(false);
  const [answer, setAnswer] = useState('');
  const [error, setError] = useState('');

  const handleUpload = async () => {
    if (!file) return;
    setUploading(true);
    setError('');
    try {
      const data = await api.docRag.upload(file, protocol);
      setUploadResult(`文档 "${data.docName}" 已索引，协议: ${data.protocol}，分块数: ${data.chunks}`);
    } catch (e: any) {
      setError(e.response?.data || e.message || '上传失败');
    } finally {
      setUploading(false);
    }
  };

  const handleAsk = async () => {
    if (!question.trim()) return;
    setLoading(true);
    setError('');
    setAnswer('');
    try {
      const data = await api.docRag.ask(question, protocol);
      setAnswer(data.answer);
    } catch (e: any) {
      setError(e.response?.data || e.message || '请求失败');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-8 max-w-5xl mx-auto">
      <header className="mb-6">
        <h2 className="text-2xl font-bold text-white">工业协议文档 RAG</h2>
        <p className="text-industrial-400 mt-1">上传 Modbus、SECS/GEM、S7、EtherNet/IP 协议文档，精准问答</p>
      </header>

      <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800 mb-6">
        <h3 className="text-sm font-medium text-industrial-300 mb-3">上传协议文档</h3>
        <div className="flex gap-4 items-center">
          <input
            type="file"
            accept=".pdf,.docx"
            onChange={e => setFile(e.target.files?.[0] || null)}
            className="flex-1 text-sm text-industrial-300 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:bg-industrial-700 file:text-white file:cursor-pointer"
          />
          <select
            value={protocol}
            onChange={e => setProtocol(e.target.value)}
            className="bg-industrial-950 border border-industrial-700 rounded-lg px-3 py-2 text-white"
          >
            {protocols.map(p => <option key={p} value={p}>{p}</option>)}
          </select>
          <button
            onClick={handleUpload}
            disabled={!file || uploading}
            className="px-5 py-2 bg-industrial-600 hover:bg-industrial-500 disabled:bg-industrial-800 disabled:text-industrial-500 text-white rounded-lg font-medium"
          >
            {uploading ? '上传中...' : '上传并索引'}
          </button>
        </div>
        {uploadResult && <p className="mt-3 text-sm text-green-400">{uploadResult}</p>}
      </div>

      <div className="bg-industrial-900 rounded-lg p-6 border border-industrial-800">
        <label className="block text-sm font-medium text-industrial-300 mb-2">协议问答</label>
        <textarea
          value={question}
          onChange={e => setQuestion(e.target.value)}
          placeholder="例如：读保持寄存器功能码 03 的报文格式是什么？"
          className="w-full h-24 bg-industrial-950 border border-industrial-700 rounded-lg px-4 py-3 text-white placeholder-industrial-500 focus:outline-none focus:border-industrial-500 resize-none"
        />
        <button
          onClick={handleAsk}
          disabled={loading || !question.trim()}
          className="mt-3 px-6 py-2.5 bg-industrial-600 hover:bg-industrial-500 disabled:bg-industrial-800 disabled:text-industrial-500 text-white rounded-lg font-medium"
        >
          {loading ? '检索中...' : '提问'}
        </button>
      </div>

      {error && <div className="mt-4 p-4 bg-red-900/30 border border-red-700 rounded-lg text-red-300">{error}</div>}
      {answer && (
        <div className="mt-6 bg-industrial-900 rounded-lg p-6 border border-industrial-800">
          <h3 className="text-sm font-medium text-industrial-400 mb-3">回答</h3>
          <pre className="whitespace-pre-wrap text-sm text-industrial-100 font-sans leading-relaxed">{answer}</pre>
        </div>
      )}
    </div>
  );
}
