import { Trans, useTranslation } from 'react-i18next';

export function Translate() {
  const { t: translate } = useTranslation();

  return <Trans t={translate}>Hello World</Trans>;
}
