import * as React from "react";
import { cn } from "@/lib/utils";

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, ...props }, ref) => {
    return (
      <input
        type={type}
        className={cn(
          "flex h-10 w-full rounded-[10px] border border-input bg-card px-3 py-2 text-sm text-foreground shadow-sm ring-offset-background transition-colors tabular",
          "file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground/80",
          "hover:border-foreground/25 focus-visible:outline-none focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-ring/30",
          "disabled:cursor-not-allowed disabled:bg-muted disabled:text-muted-foreground disabled:opacity-70 disabled:shadow-none",
          className
        )}
        ref={ref}
        {...props}
      />
    );
  }
);
Input.displayName = "Input";

export { Input };
