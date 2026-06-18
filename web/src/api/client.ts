import axios from 'axios';

const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5000/api';

const client = axios.create({
  baseURL: API_BASE,
  timeout: 600000,
  headers: { 'Content-Type': 'application/json' },
});

export interface CodeQaResponse {
  answer: string;
  references: Array<{
    id: string;
    score: number;
    content: string;
    metadata: Record<string, string>;
  }>;
}

export interface CodeGenResponse {
  code: string;
  targetPath: string;
  language: string;
  writtenToDisk: boolean;
}

export interface ScaffoldFile {
  path: string;
  content: string;
  template: string;
}

export interface ScaffoldPlanResponse {
  moduleName: string;
  summary: string;
  files: ScaffoldFile[];
}

export interface ScaffoldExecuteResponse {
  createdFiles: string[];
  diRegistrationSnippet: string;
}

export interface IndexProjectResponse {
  totalDocuments: number;
  totalChunks: number;
  failedDocuments: number;
  errors: string[];
}

export const api = {
  codeQa: {
    ask: (question: string, filePathFilter?: string, kindFilter?: string) =>
      client.post<CodeQaResponse>('/codeqa/ask', { question, filePathFilter, kindFilter }).then(r => r.data),
  },
  docRag: {
    upload: (file: File, protocol: string) => {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('protocol', protocol);
      return client.post('/docrag/upload', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      }).then(r => r.data);
    },
    ask: (question: string, protocol?: string) =>
      client.post('/docrag/ask', { question, protocol }).then(r => r.data),
  },
  codeGen: {
    generate: (type: string, requirement: string, moduleName: string, writeToDisk = false) =>
      client.post<CodeGenResponse>('/codegen/generate', { type, requirement, moduleName, writeToDisk }).then(r => r.data),
  },
  logDiagnose: {
    analyze: (logContent: string, stackTrace?: string) =>
      client.post('/logdiagnose/analyze', { logContent, stackTrace }).then(r => r.data),
  },
  scaffold: {
    plan: (requirement: string) =>
      client.post<ScaffoldPlanResponse>('/scaffold/plan', { requirement }).then(r => r.data),
    planTemplate: (requirement: string) =>
      client.post<ScaffoldPlanResponse>('/scaffold/plan-template', { requirement }).then(r => r.data),
    execute: (targetDir: string, plan: ScaffoldPlanResponse) =>
      client.post<ScaffoldExecuteResponse>('/scaffold/execute', { targetDir, plan }).then(r => r.data),
  },
  index: {
    project: (solutionOrProjectPath: string, forceReindex = false) =>
      client.post<IndexProjectResponse>('/index/project', { solutionOrProjectPath, forceReindex }).then(r => r.data),
    status: () => client.get('/index/status').then(r => r.data),
  },
};

export default client;
