import type { Meta, StoryObj } from '@storybook/react';
import React from 'react';
import { action } from 'storybook/actions';
import ButtonUpload from './ButtonUpload.tsx';

const meta: Meta<typeof ButtonUpload> = {
  title: 'Components/Button/ButtonUpload',
  component: ButtonUpload,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    className: {
      control: 'text'
    },
    disabled: {
      control: 'boolean'
    },
    onFileChange: {
      action: 'file-changed'
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    id: 'upload',
    onFileChange: action('file-changed')
  }
};

export const Disabled: Story = {
  args: {
    id: 'upload-disabled',
    disabled: true,
    onFileChange: action('file-changed')
  }
};

export const Interactive: Story = {
  args: {
    id: 'upload-interactive',
    onFileChange: (file: File | undefined) => {
      if (file) {
        console.log('File selected:', file.name, file.type, file.size);
        action('file-changed')(file);
      } else {
        console.log('No file selected');
        action('file-changed')(undefined);
      }
    }
  },
  render: (args) => (
    <div className="space-y-4">
      <div className="text-sm text-neutral-600">
        Click the upload button to select an image file. Check the console and Actions tab for file details.
      </div>
      <ButtonUpload {...args} />
      <div className="text-xs text-neutral-500">Accepts: JPEG, PNG, GIF images only</div>
    </div>
  )
};
export const WithFilePreview: Story = {
  render: () => {
    const [selectedFile, setSelectedFile] = React.useState<File | undefined>();
    const [previewUrl, setPreviewUrl] = React.useState<string | undefined>();

    const handleFileChange = (file: File | undefined) => {
      setSelectedFile(file);
      action('file-changed')(file);

      if (file) {
        const url = URL.createObjectURL(file);
        setPreviewUrl(url);
      } else {
        setPreviewUrl(undefined);
      }
    };

    React.useEffect(
      () => () => {
        if (previewUrl) {
          URL.revokeObjectURL(previewUrl);
        }
      },
      [previewUrl]
    );

    return (
      <div className="space-y-4">
        <div className="text-center">
          <div className="w-32 h-32 mx-auto border-2 border-dashed border-neutral-300 rounded-lg flex items-center justify-center relative overflow-hidden">
            {previewUrl ? (
              <img src={previewUrl} alt="Preview" className="w-full h-full object-cover" />
            ) : (
              <span className="text-neutral-400 text-sm">No image</span>
            )}
            <div className="absolute bottom-2 right-2">
              <ButtonUpload id="preview-upload" onFileChange={handleFileChange} />
            </div>
          </div>
          {selectedFile && (
            <div className="mt-2 text-xs text-neutral-600">
              {selectedFile.name} ({Math.round(selectedFile.size / 1024)}KB)
            </div>
          )}
        </div>
      </div>
    );
  }
};
