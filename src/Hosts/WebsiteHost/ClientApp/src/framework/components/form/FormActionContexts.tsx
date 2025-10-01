import React from 'react';
import { ValidationMode } from 'react-hook-form';
import { ActionResult } from '../../actions/Actions.ts';

export const FormActionRequiredFieldsContext = React.createContext<string[]>([]);
export const FormActionContext = React.createContext<ActionResult<any, any, any> | undefined>(undefined);
export const FormActionValidationContext = React.createContext<keyof ValidationMode>('onBlur');
