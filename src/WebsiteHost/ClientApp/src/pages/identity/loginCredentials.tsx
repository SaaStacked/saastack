import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import z from 'zod';
import { LoginCredentialsAction } from '../../actions/identity/loginCredentials.ts';
import Form from '../../components/form/Form.tsx';
import FormSubmitButton from '../../components/form/formSubmitButton/FormSubmitButton.tsx';
import Input from '../../components/input/Input.tsx';


export const LoginCredentialsPage: React.FC = () => {
  const [formData, setFormData] = useState({
    username: '',
    password: ''
  });

  const login = LoginCredentialsAction();

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value
    }));
  };

  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center">
        <h1>Sign In</h1>
        <Form
          id="login-credentials"
          action={login}
          validationSchema={z.object({
            username: z.string().min(1, 'Username is required'),
            password: z.string().min(1, 'Password is required')
          })}
        >
          <Input
            name="username"
            label="Username"
            autoComplete="username"
            required
            value={formData.username}
            onChange={handleInputChange}
          />
          <Input
            type="password"
            name="password"
            label="Password"
            autoComplete="current-password"
            required
            value={formData.password}
            onChange={handleInputChange}
          />
          <FormSubmitButton />
        </Form>
        <div className="">
          <p>
            <Link to="/" className="btn btn-secondary">
              Go Home
            </Link>
          </p>
          <p>
            Don't have an account? <Link to="/identity/register-credentials">Register here</Link>
          </p>
        </div>
      </div>
    </div>
  );
};
