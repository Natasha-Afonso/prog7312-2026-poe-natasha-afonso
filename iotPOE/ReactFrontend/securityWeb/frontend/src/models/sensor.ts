export interface DeploymentNode {
  id: number;
  name: string;
  parentId: number | null;
}

export interface Sensor {
  id: number;
  deviceIdentifier: string;
  category: string;
  deploymentNodeId: number;
  deploymentNode?: DeploymentNode;
  status: string;
  lastReading?: string;
  filePath?: string;
  registeredAt?: string;
}