/// <reference types="vite/client" />
/// <reference types="vite/types/importMeta.d.ts" />

interface ImportMetaEnv {
  readonly VITE_APPLICATIONINSIGHTS_CONNECTIONSTRING: string;
  readonly VITE_WEBSITEHOSTBASEURL: string;
  readonly VITE_APIHOST1BASEURL: string;
  readonly VITE_FAKEPROVIDER_SSO_BASEURL: string;
  readonly VITE_MICROSOFT_SSO_BASEURL: string;
  readonly VITE_MICROSOFT_SSO_CLIENTID: string;
  readonly VITE_GOOGLE_SSO_BASEURL: string;
  readonly VITE_GOOGLE_SSO_CLIENTID: string;
  //EXTEND: add more variables from the .env file
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
