export interface NeuronMessage {
  id: number;
  role: 'user' | 'assistant';
  content: string;
}

