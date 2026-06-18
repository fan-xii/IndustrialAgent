import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import CodeQaPage from './pages/CodeQaPage';
import DocRagPage from './pages/DocRagPage';
import CodeGenPage from './pages/CodeGenPage';
import LogDiagnosePage from './pages/LogDiagnosePage';
import ScaffoldPage from './pages/ScaffoldPage';

const navItems = [
  { path: '/', label: '代码问答', icon: '🔍', desc: '整仓代码智能检索' },
  { path: '/doc-rag', label: '协议文档', icon: '📄', desc: '工业协议 RAG' },
  { path: '/code-gen', label: '代码生成', icon: '⚡', desc: '规范化代码生成' },
  { path: '/log-diagnose', label: '日志排查', icon: '🛠', desc: '故障根因分析' },
  { path: '/scaffold', label: '项目脚手架', icon: '🏗', desc: '一键生成模块' },
];

function App() {
  return (
    <BrowserRouter>
      <div className="flex h-screen bg-industrial-950 text-industrial-100">
        <aside className="w-64 bg-industrial-900 border-r border-industrial-800 flex flex-col">
          <div className="p-6 border-b border-industrial-800">
            <h1 className="text-lg font-bold text-white flex items-center gap-2">
              <span className="text-2xl">🏭</span>
              工业研发助手
            </h1>
            <p className="text-xs text-industrial-400 mt-1">Industrial R&D Agent</p>
          </div>
          <nav className="flex-1 p-3 space-y-1">
            {navItems.map(item => (
              <NavLink
                key={item.path}
                to={item.path}
                end={item.path === '/'}
                className={({ isActive }) =>
                  `flex items-start gap-3 px-3 py-2.5 rounded-lg transition-colors ${
                    isActive
                      ? 'bg-industrial-700 text-white'
                      : 'text-industrial-300 hover:bg-industrial-800 hover:text-white'
                  }`
                }
              >
                <span className="text-xl">{item.icon}</span>
                <div>
                  <div className="text-sm font-medium">{item.label}</div>
                  <div className="text-xs text-industrial-400">{item.desc}</div>
                </div>
              </NavLink>
            ))}
          </nav>
          <div className="p-4 border-t border-industrial-800 text-xs text-industrial-500">
            <p>GLM-5.2 [1M] · Semantic Kernel</p>
            <p className="mt-1">Qdrant · Roslyn · Prism MVVM</p>
          </div>
        </aside>
        <main className="flex-1 overflow-auto">
          <Routes>
            <Route path="/" element={<CodeQaPage />} />
            <Route path="/doc-rag" element={<DocRagPage />} />
            <Route path="/code-gen" element={<CodeGenPage />} />
            <Route path="/log-diagnose" element={<LogDiagnosePage />} />
            <Route path="/scaffold" element={<ScaffoldPage />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}

export default App;
