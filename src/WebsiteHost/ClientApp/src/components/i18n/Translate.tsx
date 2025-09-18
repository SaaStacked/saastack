import { Trans, useTranslation } from 'react-i18next';

export function Translate() {
  const { t: translate } = useTranslation('myNamespace');

  return <Trans t={translate}>Hello World</Trans>;
}
