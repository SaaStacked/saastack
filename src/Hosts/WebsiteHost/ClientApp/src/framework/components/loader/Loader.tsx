import React from 'react';
import { createComponentId, toClasses } from '../Components';


export interface LoaderProps {
  id?: string;
  message: string;
  type: 'inline' | 'page';
  className?: string;
}

// Creates a spinning loader in the middle of the screen
const Loader: React.FC<LoaderProps> = ({ id, message, type, className }) => {
  const isInline = type === 'inline';

  const baseClasses = 'flex items-center justify-center';
  const layoutClasses = isInline ? 'flex-row space-x-2 h-full w-full' : 'flex-col space-y-4 h-screen';
  const classes = toClasses([baseClasses, layoutClasses, className]);

  const spinnerClasses = isInline
    ? 'rounded-full h-[1em] w-[1em] border-[0.15em] border-neutral-300 border-t-brand-secondary animate-spin flex-shrink-0'
    : 'rounded-full h-12 w-12 border-4 border-neutral-300 border-t-brand-secondary animate-spin';

  const textClasses = isInline ? 'text-neutral-600 text-[1em] whitespace-nowrap' : 'text-neutral-600';
  const loadingText = `${message.replace(/\.+$/, '')}`;
  const componentId = createComponentId('loader', id);

  return (
    <div className={classes}>
      <div className={spinnerClasses}></div>
      <p data-testid={componentId} className={textClasses}>
        {loadingText}
        <span>...</span> {/*needed for unit testing*/}
      </p>
    </div>
  );
};

export default Loader;
