import { Trans, useTranslation } from 'react-i18next';


export function Translate() {
  const { t: translate } = useTranslation('common');

  return <Trans t={translate}>Hello World</Trans>;
}
