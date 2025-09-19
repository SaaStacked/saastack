import React from 'react';
import { createComponentId } from '../Components';

export interface LoaderProps {
  id?: string;
  message: string;
}

// Creates a spinning loader in the middle of the screen
const Loader: React.FC<LoaderProps> = ({ id, message }) => {
  const componentId = createComponentId('loader', id);
  return (
    <div className="flex flex-col items-center space-y-4 items-center justify-center h-screen">
      <div className="rounded-full h-12 w-12 border-4 border-gray-300 border-t-blue-500 animate-spin"></div>
      <p data-testid={componentId} className="text-gray-600">
        {message}
      </p>
    </div>
  );
};

export default Loader;
