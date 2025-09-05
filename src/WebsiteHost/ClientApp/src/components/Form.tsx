import React from 'react';

export interface FormProps {
  className?: string;
  children: React.ReactNode;
  action: string;
}

const Form: React.FC<FormProps> = ({ className = '', children, action }) => {
  const baseClasses = 'bg-white rounded-lg transition-all';
  const classes = [baseClasses, className].filter(Boolean).join(' ');

  return (
    <form
      className={classes}
      //      onSubmit={action} //TODO: we need to invoke the action with the form data
    >
      <div>{children}</div>
    </form>
  );
};

export default Form;
