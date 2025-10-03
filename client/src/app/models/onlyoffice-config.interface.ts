import { IConfig } from '@onlyoffice/document-editor-angular';

export interface IOnlyOfficeConfig {
  config: IConfig;
  onlyOfficeServerUrl: string;
  userId: string;
}