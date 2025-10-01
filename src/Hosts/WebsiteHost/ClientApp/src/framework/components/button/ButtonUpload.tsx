import React from 'react';
import { createComponentId, toClasses } from '../Components.ts';
import Icon from '../icon/Icon.tsx';


export interface UploadButtonProps {
  className?: string;
  id?: string;
  onFileChange?: (file: File | undefined) => void;
  disabled?: boolean;
}

// Creates a button that allows the user to upload a file
// Accepts a callback that is invoked when the user selects a file
export default function ButtonUpload({ className, id, onFileChange, disabled }: UploadButtonProps) {
  const baseClasses = 'p-2 rounded-full flex items-center justify-center';
  const disabledClasses = disabled
    ? 'cursor-not-allowed bg-gray-400 text-gray-600  opacity-50'
    : 'cursor-pointer bg-blue-500 hover:bg-blue-600 text-white';
  const classes = toClasses([baseClasses, disabledClasses, className]);
  const componentId = createComponentId('upload_button', id);
  const [_file, setFile] = React.useState<File | undefined>(undefined);

  const handleFileChange = (newFile: File | undefined) => {
    setFile(newFile);
    onFileChange?.(newFile);
  };

  return (
    <>
      <label className={classes} data-testid={componentId}>
        <Icon symbol="edit" size={14} color={disabled ? 'gray-600' : 'white'} />
        <input
          data-testid={`${componentId}_file_input`}
          type="file"
          accept="image/jpeg,image/png,image/gif"
          onChange={async (event: React.ChangeEvent<HTMLInputElement>) => handleFileChange(event.target.files?.[0])}
          disabled={disabled}
          className="hidden"
        />
      </label>
    </>
  );
}
