/// <reference types="vite/client" />
/// <reference types="vite/types/importMeta.d.ts" />

interface ImportMetaEnv {
  readonly VITE_APPLICATIONINSIGHTS_CONNECTIONSTRING: string;
  readonly VITE_WEBSITEHOSTBASEURL: string;
  readonly VITE_APIHOST1BASEURL: string;
  //EXTEND: add more variables from the .env file
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
