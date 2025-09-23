import React from 'react';
import { createComponentId } from '../Components';

export interface CardProps {
  className?: string;
  id?: string;
  children: React.ReactNode;
}

function Card({ className, id, children }: CardProps) {
  const baseClasses = 'container min-h-screen flex items-center justify-center';
  const classes = [baseClasses, className].filter(Boolean).join(' ');
  const componentId = createComponentId('card', id);
  return (
    <>
      <div className={classes} data-testid={componentId}>
        <div className="rounded-2xl shadow-2xl p-8 bg-white dark:bg-gray-800 lg:w-3/5 md:w-3/5 w-11/12">{children}</div>
      </div>
    </>
  );
}

export default Card;
