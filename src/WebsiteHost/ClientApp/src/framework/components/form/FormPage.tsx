import React from 'react';
import { createComponentId, toClasses } from '../Components';


export interface CardProps {
  className?: string;
  id?: string;
  children: React.ReactNode;
  title?: string;
  align?: 'middle' | 'top';
}

// Creates a page to display a form in the middle of the screen
function FormPage({ className, id, children, title, align = 'middle' }: CardProps) {
  const baseClasses = 'container flex items-center justify-center';
  const sizeClasses = align === 'top' ? 'h-screen items-start pt-16' : 'min-h-screen items-center';
  const classes = toClasses([baseClasses, sizeClasses, className]);
  const componentId = createComponentId('card', id);
  return (
    <>
      <div className={classes} data-testid={componentId}>
        <div className="rounded-2xl shadow-2xl p-8 bg-white dark:bg-gray-800 lg:w-3/5 md:w-3/5 w-11/12">
          {title && <h1 className="text-4xl font-bold text-center mb-16">{title}</h1>}
          {children}
        </div>
      </div>
    </>
  );
}

export default FormPage;
