import React from 'react';
import { createComponentId, toClasses } from '../Components';


export interface CardProps {
  className?: string;
  id?: string;
  children: React.ReactNode;
  title?: string;
  align?: 'middle' | 'top';
  width?: 'default' | 'wide' | 'full';
}

// Creates a page to display a form in the middle of the screen
function FormPage({ className, id, children, title, align = 'middle', width = 'default' }: CardProps) {
  const baseClasses = 'container flex items-center justify-center';
  const sizeClasses = align === 'top' ? 'min-h-screen items-start pt-16' : 'min-h-screen items-center';
  const widthClasses = {
    default: 'w-11/12 md:w-3/5',
    wide: 'w-11/12 md:w-4/5',
    full: 'w-12/12'
  };

  const classes = toClasses([baseClasses, sizeClasses, className]);
  const componentId = createComponentId('card', id);

  return (
    <>
      <div className={classes} data-testid={componentId}>
        <div className={`rounded-lg shadow-2xl p-8 bg-white dark:bg-neutral-800 ${widthClasses[width]}`}>
          {title && (
            <h1 className="text-3xl font-medium text-neutral-900 dark:text-neutral-50 text-center mb-12">{title}</h1>
          )}
          {children}
        </div>
      </div>
    </>
  );
}

export default FormPage;
